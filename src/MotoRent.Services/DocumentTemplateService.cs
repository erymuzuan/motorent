using System.Text.Json;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;
using MotoRent.Domain.Storage;

namespace MotoRent.Services;

/// <summary>
/// Service for managing document templates and their layouts.
/// </summary>
public class DocumentTemplateService(RentalDataContext context, IBinaryStore binaryStore)
{
    private RentalDataContext Context { get; } = context;
    private IBinaryStore BinaryStore { get; } = binaryStore;

    /// <summary>
    /// Saves a template and its layout.
    /// </summary>
    public async Task<SubmitOperation> SaveTemplateAsync(DocumentTemplate template, DocumentLayout layout, string username)
    {
        // 1. Serialize layout to JSON
        var json = JsonSerializer.Serialize(layout);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // 2. Prepare store ID if not exists
        if (string.IsNullOrWhiteSpace(template.StoreId))
        {
            var guid = Guid.NewGuid().ToString("N");
            template.StoreId = $"templates/{template.Type}/{guid}.json";
        }

        // 3. Save to IBinaryStore
        var binary = new BinaryStore
        {
            StoreId = template.StoreId,
            Content = bytes,
            ContentType = "application/json",
            FileName = $"{template.Name}.json",
            Extension = ".json"
        };
        await this.BinaryStore.AddAsync(binary);

        // 4. Save metadata to SQL
        using var session = this.Context.OpenSession(username);
        
        // If it's set as default, unset others of same type/shop
        if (template.IsDefault)
        {
            var others = await this.Context.LoadAsync(this.Context.CreateQuery<DocumentTemplate>()
                .Where(t => t.Type == template.Type && t.IsDefault && t.DocumentTemplateId != template.DocumentTemplateId && t.ShopId == template.ShopId));
            
            foreach (var other in others.ItemCollection)
            {
                other.IsDefault = false;
                session.Attach(other);
            }
        }

        session.Attach(template);
        return await session.SubmitChanges("SaveDocumentTemplate");
    }

    /// <summary>
    /// Loads a template's layout from binary storage.
    /// </summary>
    public async Task<DocumentLayout?> GetTemplateLayoutAsync(string storeId)
    {
        var content = await this.BinaryStore.GetContentAsync(storeId);
        if (content?.Content == null) return null;

        var json = System.Text.Encoding.UTF8.GetString(content.Content);
        return JsonSerializer.Deserialize<DocumentLayout>(json);
    }

    /// <summary>
    /// Gets the default template for a document type and optional shop.
    /// Prioritizes shop-specific defaults over global ones.
    /// </summary>
    public async Task<DocumentTemplate?> GetDefaultTemplateAsync(DocumentType type, int? shopId = null)
    {
        // 1. Try shop-specific default
        if (shopId > 0)
        {
            var shopDefault = await this.Context.LoadOneAsync<DocumentTemplate>(
                t => t.Type == type && t.IsDefault && t.Status == DocumentTemplateStatus.Approved && t.ShopId == shopId);
            
            if (shopDefault != null) return shopDefault;
        }

        // 2. Fallback to global default (ShopId null or 0)
        return await this.Context.LoadOneAsync<DocumentTemplate>(
            t => t.Type == type && t.IsDefault && t.Status == DocumentTemplateStatus.Approved && (t.ShopId == null || t.ShopId == 0));
    }

    /// <summary>
    /// Gets the effective default template, falling back to the latest approved if no default is marked.
    /// </summary>
    public async Task<DocumentTemplate?> GetEffectiveDefaultTemplateAsync(DocumentType type, int? shopId = null)
    {
        var template = await GetDefaultTemplateAsync(type, shopId);
        if (template != null) return template;

        // Fallback to latest approved
        var query = this.Context.CreateQuery<DocumentTemplate>()
            .Where(t => t.Type == type && t.Status == DocumentTemplateStatus.Approved);
        
        if (shopId > 0) query = query.Where(t => t.ShopId == null || t.ShopId == 0 || t.ShopId == shopId);

        var result = await this.Context.LoadAsync(query.OrderByDescending(t => t.DocumentTemplateId), page: 1, size: 1);
        return result.ItemCollection.FirstOrDefault();
    }

    /// <summary>
    /// Gets all templates for a specific document type and optional shop filter.
    /// </summary>
    public async Task<LoadOperation<DocumentTemplate>> GetTemplatesByTypeAsync(DocumentType type, int? shopId = null, int page = 1, int size = 100)
    {
        var query = this.Context.CreateQuery<DocumentTemplate>()
            .Where(t => t.Type == type);

        if (shopId > 0)
        {
            query = query.Where(t => t.ShopId == null || t.ShopId == 0 || t.ShopId == shopId);
        }

        query = query.OrderByDescending(t => t.IsDefault);
            // .ThenByDescending(t => t.ShopId > 0)
            // .ThenBy(t => t.Name);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Gets all templates with optional type filter.
    /// </summary>
    public async Task<LoadOperation<DocumentTemplate>> GetTemplatesAsync(DocumentType? type = null, int page = 1, int size = 100)
    {
        var query = this.Context.CreateQuery<DocumentTemplate>();
        
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        query = query.OrderByDescending(t => t.DocumentTemplateId);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Seeds default templates for a shop or organization if none exist.
    /// </summary>
    public async Task SeedDefaultTemplatesAsync(int? shopId, string username)
    {
        // 1. Check if templates already exist for this context
        var existing = await this.Context.LoadAsync(this.Context.CreateQuery<DocumentTemplate>()
            .Where(t => t.ShopId == shopId));
        
        if (existing.ItemCollection.Any()) return;

        // 2. Create defaults for each type
        await this.CreateDefaultBookingConfirmation(shopId, username);
        await this.CreateDefaultRentalAgreement(shopId, username);
        await this.CreateDefaultReceipt(shopId, username);
    }

    private async Task CreateDefaultBookingConfirmation(int? shopId, string username)
    {
        var layout = new DocumentLayout
        {
            Sections = new List<LayoutSection>
            {
                new LayoutSection
                {
                    Name = "Header",
                    Blocks = new List<LayoutBlock>
                    {
                        new HeadingBlock { Content = "Booking Confirmation", Level = 1, HorizontalAlignment = "Center", IsBold = true },
                        new TextBlock { Content = "{{Org.Name}}", HorizontalAlignment = "Center", FontSize = 14 },
                        new DividerBlock { Thickness = 2, Color = "#000000" }
                    }
                },
                new LayoutSection
                {
                    Name = "Customer Info",
                    Blocks = new List<LayoutBlock>
                    {
                        new HeadingBlock { Content = "Customer Details", Level = 3 },
                        new TextBlock { Content = "Name: {{Booking.CustomerName}}\nPhone: {{Booking.CustomerPhone}}\nEmail: {{Booking.CustomerEmail}}" }
                    }
                },
                new LayoutSection
                {
                    Name = "Booking Details",
                    Blocks = new List<LayoutBlock>
                    {
                        new HeadingBlock { Content = "Reservation Info", Level = 3 },
                        new TextBlock { Content = "Reference: {{Booking.Ref}}\nPickup: {{Booking.StartDate}}\nReturn: {{Booking.EndDate}}\nDuration: {{Booking.Days}} Days" },
                        new SpacerBlock { Height = 10 },
                        new TableBlock 
                        {
                            BindingPath = "Booking.Items",
                            Columns = new List<TableColumn>
                            {
                                new TableColumn { Header = "Vehicle", BindingPath = "VehicleDisplayName" },
                                new TableColumn { Header = "Amount", BindingPath = "ItemTotal", HorizontalAlignment = "Right" }
                            }
                        }
                    }
                },
                new LayoutSection
                {
                    Name = "Totals",
                    Blocks = new List<LayoutBlock>
                    {
                        new DividerBlock { Thickness = 1 },
                        new TextBlock { Content = "Total Amount: {{Booking.TotalAmount}}", HorizontalAlignment = "Right", IsBold = true },
                        new TextBlock { Content = "Deposit Required: {{Booking.DepositRequired}}", HorizontalAlignment = "Right" }
                    }
                }
            }
        };

        var template = new DocumentTemplate
        {
            Name = "Standard Booking Confirmation",
            Type = DocumentType.BookingConfirmation,
            Status = DocumentTemplateStatus.Approved,
            IsDefault = true,
            ShopId = shopId
        };

        await this.SaveTemplateAsync(template, layout, username);
    }

    private async Task CreateDefaultRentalAgreement(int? shopId, string username)
    {
        var layout = new DocumentLayout
        {
            Sections = new List<LayoutSection>
            {
                new LayoutSection
                {
                    Name = "Header",
                    Blocks = new List<LayoutBlock>
                    {
                        new HeadingBlock { Content = "Rental Agreement", Level = 1, HorizontalAlignment = "Center", IsBold = true },
                        new TextBlock { Content = "{{Org.Name}}", HorizontalAlignment = "Center", FontSize = 14 },
                        new TextBlock { Content = "{{Org.Address}}", HorizontalAlignment = "Center", FontSize = 10 }
                    }
                },
                new LayoutSection
                {
                    Name = "Info",
                    Blocks = new List<LayoutBlock>
                    {
                        new TwoColumnBlock
                        {
                            LeftColumn = new List<LayoutBlock>
                            {
                                new HeadingBlock { Content = "Renter", Level = 4 },
                                new TextBlock { Content = "{{Rental.CustomerName}}" }
                            },
                            RightColumn = new List<LayoutBlock>
                            {
                                new HeadingBlock { Content = "Vehicle", Level = 4 },
                                new TextBlock { Content = "{{Rental.VehicleName}}" }
                            }
                        }
                    }
                },
                new LayoutSection
                {
                    Name = "Terms",
                    Blocks = new List<LayoutBlock>
                    {
                        new HeadingBlock { Content = "Terms & Conditions", Level = 4 },
                        new TextBlock { Content = "1. The renter agrees to return the vehicle in the same condition.\n2. Insurance coverage is subject to the selected plan.\n3. Late returns will be charged at the daily rate.", FontSize = 9 }
                    }
                },
                new LayoutSection
                {
                    Name = "Signatures",
                    Blocks = new List<LayoutBlock>
                    {
                        new SpacerBlock { Height = 30 },
                        new TwoColumnBlock
                        {
                            LeftColumn = new List<LayoutBlock> { new SignatureBlock { Label = "Renter's Signature" } },
                            RightColumn = new List<LayoutBlock> { new SignatureBlock { Label = "Shop Representative" } }
                        }
                    }
                }
            }
        };

        var template = new DocumentTemplate
        {
            Name = "Standard Rental Agreement",
            Type = DocumentType.RentalAgreement,
            Status = DocumentTemplateStatus.Approved,
            IsDefault = true,
            ShopId = shopId
        };

        await this.SaveTemplateAsync(template, layout, username);
    }

    private async Task CreateDefaultReceipt(int? shopId, string username)
    {
        var layout = new DocumentLayout
        {
            Sections =
            [
                new LayoutSection
                {
                    Name = "Header",
                    Blocks =
                    [
                        new HeadingBlock
                            { Content = "RECEIPT", Level = 2, HorizontalAlignment = "Center", IsBold = true },

                        new TextBlock { Content = "{{Org.Name}}", HorizontalAlignment = "Center" }
                    ]
                },

                new LayoutSection
                {
                    Name = "Details",
                    Blocks = new List<LayoutBlock>
                    {
                        new TextBlock
                        {
                            Content =
                                "Receipt No: {{Receipt.No}}\nDate: {{Receipt.Date}}\nCustomer: {{Receipt.CustomerName}}"
                        },
                        new SpacerBlock { Height = 10 },
                        new TableBlock
                        {
                            BindingPath = "Receipt.Items",
                            Columns = new List<TableColumn>
                            {
                                new TableColumn { Header = "Description", BindingPath = "Description" },
                                new TableColumn
                                    { Header = "Amount", BindingPath = "Total", HorizontalAlignment = "Right" }
                            }
                        }
                    }
                },

                new LayoutSection
                {
                    Name = "Footer",
                    Blocks = new List<LayoutBlock>
                    {
                        new DividerBlock { Thickness = 1 },
                        new TextBlock
                        {
                            Content = "Grand Total: {{Receipt.TotalAmount}}", HorizontalAlignment = "Right",
                            IsBold = true, FontSize = 14
                        },
                        new SpacerBlock { Height = 20 },
                        new TextBlock
                        {
                            Content = "Thank you for choosing {{Org.Name}}!", HorizontalAlignment = "Center",
                            FontSize = 10
                        }
                    }
                }
            ]
        };

        var template = new DocumentTemplate
        {
            Name = "Standard Receipt",
            Type = DocumentType.Receipt,
            Status = DocumentTemplateStatus.Approved,
            IsDefault = true,
            ShopId = shopId
        };

        await this.SaveTemplateAsync(template, layout, username);
    }

    /// <summary>
    /// Gets a single template by its ID.
    /// </summary>
    public async Task<DocumentTemplate?> GetTemplateByIdAsync(int templateId)
    {
        return await this.Context.LoadOneAsync<DocumentTemplate>(t => t.DocumentTemplateId == templateId);
    }
}
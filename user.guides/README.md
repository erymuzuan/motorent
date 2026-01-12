# MotoRent User Guides

Quick start guides for all MotoRent user roles.

## Available Guides

| Guide | Role | Access Level |
|-------|------|--------------|
| [OrgAdmin Quick Start](01-orgadmin-quickstart.md) | Organization Admin | Full access - all features |
| [Staff Quick Start](02-staff-quickstart.md) | Staff | Rentals, Accidents, Customers |
| [Mechanic Quick Start](03-mechanic-quickstart.md) | Mechanic | Fleet (view), Dashboard |
| [ShopManager Quick Start](04-shopmanager-quickstart.md) | Shop Manager | Full access - shop operations |

## Role Hierarchy

```
OrgAdmin (Organization Admin)
├── Full system access
├── All shops in organization
└── User management

ShopManager
├── Full shop access
├── Staff supervision
└── Local settings

Staff
├── Rental operations
├── Customer management
└── Accident reporting

Mechanic
├── Fleet viewing
└── Maintenance tracking
```

## Feature Access by Role

| Feature | OrgAdmin | ShopManager | Staff | Mechanic |
|---------|:--------:|:-----------:|:-----:|:--------:|
| Dashboard | Yes | Yes | Yes | Yes |
| Rentals | Yes | Yes | Yes | - |
| Check-In/Out | Yes | Yes | Yes | - |
| Finance | Yes | Yes | - | - |
| Payments | Yes | Yes | - | - |
| Deposits | Yes | Yes | - | - |
| Reports | Yes | Yes | - | - |
| Fleet | Yes | Yes | - | View |
| Vehicles | Yes | Yes | - | View |
| Accessories | Yes | Yes | - | View |
| Accidents | Yes | Yes | Yes | - |
| Customers | Yes | Yes | Yes | - |
| Settings | Yes | Yes | - | - |
| Insurance | Yes | Yes | - | - |
| Shops | Yes | Yes | - | - |

## Getting Started

1. **Identify your role** - Check with your administrator
2. **Read your guide** - Start with the quick start for your role
3. **Practice** - Use the test environment if available
4. **Ask questions** - Contact your manager or admin

## Support

- **Documentation**: Click "Documentation" in the app footer
- **Help**: Click "Help" in the app footer
- **Admin Contact**: Contact your Organization Admin

---

*Last Updated: January 2026*

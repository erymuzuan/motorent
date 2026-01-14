# Safe & Go Quick Start Guide - Super Admin

Welcome to Safe & Go! This guide covers the Super Admin (Platform Administrator) role.

![Impersonate User](images/01-impersonate.png)

## Your Role

As a Super Admin, you manage the entire Safe & Go platform:
- Manage Organizations (tenants)
- Manage System Users
- Impersonate users for support
- Handle Registration Invites
- View System Logs
- Configure Global Settings

> **Note:** Super Admin is a platform-level role. Without impersonation, you won't see tenant menus (Rentals, Fleet, etc.). Use the impersonation feature to access tenant-specific features.

## Accessing Super Admin Features

### Navigation

Super Admin pages are accessed through the user dropdown menu:
1. Click your profile icon in the top-right corner
2. Select "Super Admin" or navigate directly to `/super-admin/*` pages

### Super Admin Pages

| Page | URL | Purpose |
|------|-----|---------|
| Organizations | `/super-admin/organizations` | Manage tenants |
| Users | `/super-admin/users` | Manage system users |
| Impersonate | `/super-admin/impersonate` | Support user impersonation |
| Invites | `/super-admin/invites` | Registration invite codes |
| Logs | `/super-admin/logs` | System error logs |
| Settings | `/super-admin/settings` | Global configuration |

## Managing Organizations

### Viewing Organizations

Navigate to **Super Admin > Organizations** to see all tenants:
- Organization name
- Account number (unique identifier)
- Contact information
- Status (Active/Inactive)
- Created date

### Creating an Organization

1. Click "Add Organization"
2. Fill in required details:
   - **Organization Name** - Business name
   - **Account Number** - Unique identifier (auto-generated or custom)
   - **Contact Email** - Primary contact
   - **Contact Phone** - Business phone
3. Configure initial settings
4. Click "Create"

### Editing an Organization

1. Find the organization in the list
2. Click "Edit" or the organization name
3. Update details as needed
4. Save changes

### Organization Status

- **Active** - Normal operation
- **Inactive** - Suspended (users cannot log in)

## Managing Users

### Viewing Users

Navigate to **Super Admin > Users** to see all system users:
- User name and email
- Authentication provider (Google, Microsoft)
- Associated organizations
- Role within each organization
- Last login date

### User Accounts

Each user can belong to multiple organizations with different roles:

| Role | Description |
|------|-------------|
| OrgAdmin | Full access to organization |
| ShopManager | Manages shop operations |
| Staff | Handles daily rentals |
| Mechanic | Fleet maintenance |

### Adding Users to Organizations

1. Find the user
2. Click "Manage Access"
3. Select an organization
4. Assign a role
5. Save changes

## Impersonating Users

The impersonation feature allows you to sign in as another user for support purposes.

### How to Impersonate

1. Navigate to **Super Admin > Impersonate**
2. Search for a user by:
   - Username
   - Email
   - Name
3. Click the organization badge to impersonate
4. You are now signed in as that user

### During Impersonation

- You see the system exactly as that user does
- Your actions are logged as the impersonated user
- A banner indicates you're impersonating
- Click "Stop Impersonating" to return to your account

### When to Use Impersonation

- **Troubleshooting** - See what users see
- **Support** - Help users with specific issues
- **Testing** - Verify features work correctly
- **Training** - Demonstrate features

### Best Practices

1. Only impersonate when necessary
2. Don't make changes unless requested
3. End impersonation when done
4. Document support interactions

## Registration Invites

Invite codes allow new organizations to register on the platform.

### Creating Invites

1. Navigate to **Super Admin > Invites**
2. Click "Create Invite"
3. Configure:
   - **Code** - Custom or auto-generated
   - **Uses** - Maximum number of uses
   - **Expiry** - Expiration date
   - **Organization Template** - Pre-configured settings
4. Click "Create"

### Managing Invites

- View active and expired invites
- See usage statistics
- Revoke unused invites
- Extend expiration dates

### Invite Workflow

1. Create invite code
2. Share with new customer
3. Customer registers using code
4. Organization is created
5. Customer sets up their account

## System Logs

View and analyze system events and errors.

### Viewing Logs

Navigate to **Super Admin > Logs** to see:
- Error messages
- Stack traces
- Affected users/organizations
- Timestamps

### Filtering Logs

Filter by:
- **Level** - Error, Warning, Info
- **Date Range** - Custom time period
- **Organization** - Specific tenant
- **User** - Specific user

### Common Log Types

| Type | Description |
|------|-------------|
| Error | System errors requiring attention |
| Warning | Potential issues |
| Info | General system events |
| Audit | User actions (login, changes) |

## System Settings

Configure global platform settings.

### General Settings

- Platform name and branding
- Default language
- Date/time formats
- Currency settings

### Authentication Settings

- OAuth providers (Google, Microsoft)
- Session timeout
- Password policies (if applicable)

### Email Settings

- SMTP configuration
- Email templates
- Notification preferences

### Feature Flags

Enable/disable platform features:
- Tourist portal
- Multi-location support
- Advanced reporting
- API access

## Daily Administration

### Morning Checklist

- [ ] Review system logs for errors
- [ ] Check pending registration invites
- [ ] Verify all organizations are active
- [ ] Review any support tickets

### Weekly Tasks

- [ ] Audit user access
- [ ] Review organization usage
- [ ] Check system performance
- [ ] Update documentation

### Monthly Tasks

- [ ] Generate platform reports
- [ ] Review security settings
- [ ] Plan system updates
- [ ] Backup configuration

## Security Considerations

### Access Control

1. Limit Super Admin access to essential personnel
2. Use strong authentication (OAuth)
3. Review access logs regularly
4. Remove unused accounts

### Data Protection

1. Never share user credentials
2. Use impersonation for support (not direct login)
3. Log all administrative actions
4. Follow data retention policies

### Incident Response

If you detect suspicious activity:
1. Document the issue
2. Temporarily disable affected accounts
3. Review logs for scope
4. Notify affected parties
5. Implement corrective measures

## Troubleshooting

### Common Issues

**User can't log in:**
1. Check user status (active)
2. Verify organization status
3. Check authentication provider
4. Review recent log entries

**Organization not visible:**
1. Verify organization exists
2. Check status (active/inactive)
3. Confirm user has access

**Impersonation not working:**
1. Verify Super Admin role
2. Check target user exists
3. Ensure organization is active

## Getting Help

- **Technical Documentation** - Check internal docs
- **Development Team** - Contact for system issues
- **Security Team** - Report security concerns

---

*Safe & Go - Vehicle Rental Management System*
*Platform Administration Guide*

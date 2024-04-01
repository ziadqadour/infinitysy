using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.VendorPermission
{
    public class VendorPermission : IPermissionProvider
    {
        public static readonly PermissionRecord AdvancedSettingsVisibility = new()
        {
            Name = "Advanced Settings Visibility",
            SystemName = "AdvancedSettingsVisibility",
            Category = "Standard"
        };
        /// <summary>
        /// Get permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual IEnumerable<PermissionRecord> GetPermissions()
        {
            return new[]
            {
            AdvancedSettingsVisibility
        };
        }
        /// <summary>
        /// Get default permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
        {
            return new() { (NopCustomerDefaults.AdministratorsRoleName, new[] { AdvancedSettingsVisibility }) };
        }
    }
}

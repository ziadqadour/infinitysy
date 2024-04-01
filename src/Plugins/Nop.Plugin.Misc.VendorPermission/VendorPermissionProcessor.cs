using System.Linq;
using System.Threading.Tasks;
using Nop.Services.Logging;
using Nop.Services.Plugins;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.VendorPermission
{
    /// <summary>
    /// Vendor Permission Processor for Advanced
    /// </summary>
    public class VendorPermissionProcessor : BasePlugin
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IPermissionService _permissionService;
        #endregion

        #region Ctor

        public VendorPermissionProcessor(
            IPermissionService permissionService, ILogger logger)
        {
            _permissionService=permissionService;
            _logger = logger;
           
        }

        #endregion

        #region Methods

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            await _permissionService.InstallPermissionsAsync(new VendorPermission());

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //delete permission
            var permissionRecord = (await _permissionService.GetAllPermissionRecordsAsync())
                .FirstOrDefault(x => x.SystemName == VendorPermission.AdvancedSettingsVisibility.SystemName);
            var listMappingCustomerRolePermissionRecord = await _permissionService.GetMappingByPermissionRecordIdAsync(permissionRecord.Id);
            foreach (var mappingCustomerPermissionRecord in listMappingCustomerRolePermissionRecord)
                await _permissionService.DeletePermissionRecordCustomerRoleMappingAsync(
                    mappingCustomerPermissionRecord.PermissionRecordId,
                    mappingCustomerPermissionRecord.CustomerRoleId);

            await _permissionService.DeletePermissionRecordAsync(permissionRecord);

            await base.UninstallAsync();
        }

        #endregion

        
    }
}
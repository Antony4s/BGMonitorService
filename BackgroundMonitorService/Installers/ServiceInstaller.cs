using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace BackgroundMonitorService.Installers
{
    [RunInstaller(true)]
    public partial class ServiceInstaller : System.Configuration.Install.Installer
    {
        public ServiceInstaller()
        {
            // Create a ServiceProcessInstaller instance
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem // The account under which the service runs
            };

            // Create a ServiceInstaller instance
            System.ServiceProcess.ServiceInstaller serviceInstaller = new System.ServiceProcess.ServiceInstaller
            {
                ServiceName = "BackgroundMonitorService",    // Name of the service
                DisplayName = "Background Monitor Service",  // Name shown in Service Manager
                StartType = ServiceStartMode.Automatic       // The service starts automatically with Windows
            };

            // Add installers to the collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}

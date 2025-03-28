using Pulumi;
using Pulumi.AzureNative.Compute;
using Pulumi.AzureNative.Compute.Inputs;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Network.Inputs;
using Pulumi.AzureNative.Resources;
using System.Collections.Generic;

class MyStack : Stack
{
    public MyStack()
    {
        var resourceGroup = new ResourceGroup("swarm-rg");

        var vnet = new VirtualNetwork("swarm-vnet", new()
        {
            ResourceGroupName = resourceGroup.Name,
            AddressSpace = new AddressSpaceArgs { AddressPrefixes = { "10.0.0.0/16" } }
        });

        var subnet = new Subnet("swarm-subnet", new()
        {
            ResourceGroupName = resourceGroup.Name,
            VirtualNetworkName = vnet.Name,
            AddressPrefix = "10.0.1.0/24"
        });

        var publicIps = new List<PublicIPAddress>();
        var nics = new List<NetworkInterface>();
        var vmNames = new[] { "manager", "worker1", "worker2" };

        for (int i = 0; i < vmNames.Length; i++)
        {
            var publicIp = new PublicIPAddress($"{vmNames[i]}-ip", new()
            {
                ResourceGroupName = resourceGroup.Name,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic
            });
            publicIps.Add(publicIp);

            var nic = new NetworkInterface($"{vmNames[i]}-nic", new()
            {
                ResourceGroupName = resourceGroup.Name,
                IpConfigurations =
                {
                    new NetworkInterfaceIPConfigurationArgs
                    {
                        Name = "ipconfig1",
                        Subnet = new SubnetArgs { Id = subnet.Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressArgs { Id = publicIp.Id }
                    }
                }
            });
            nics.Add(nic);
        }

        var adminUsername = "azureuser";
        var sshPublicKey = System.IO.File.ReadAllText(System.Environment.GetEnvironmentVariable("HOME") + "/.ssh/id_rsa.pub");

        var vmList = new List<VirtualMachine>();

        for (int i = 0; i < vmNames.Length; i++)
        {
            var vm = new VirtualMachine(vmNames[i], new()
            {
                ResourceGroupName = resourceGroup.Name,
                NetworkProfile = new NetworkProfileArgs
                {
                    NetworkInterfaces = { new NetworkInterfaceReferenceArgs { Id = nics[i].Id } }
                },
                HardwareProfile = new HardwareProfileArgs { VmSize = VirtualMachineSizeTypes.StandardB1s },
                OsProfile = new OSProfileArgs
                {
                    ComputerName = vmNames[i],
                    AdminUsername = adminUsername,
                    LinuxConfiguration = new LinuxConfigurationArgs
                    {
                        DisablePasswordAuthentication = true,
                        Ssh = new SshConfigurationArgs
                        {
                            PublicKeys = {
                                new SshPublicKeyArgs
                                {
                                    KeyData = sshPublicKey,
                                    Path = $"/home/{adminUsername}/.ssh/authorized_keys"
                                }
                            }
                        }
                    }
                },
                StorageProfile = new StorageProfileArgs
                {
                    OsDisk = new OSDiskArgs
                    {
                        CreateOption = DiskCreateOptionTypes.FromImage,
                        ManagedDisk = new ManagedDiskParametersArgs { StorageAccountType = StorageAccountTypes.StandardLRS },
                        Name = $"{vmNames[i]}-osdisk"
                    },
                    ImageReference = new ImageReferenceArgs
                    {
                        Publisher = "Canonical",
                        Offer = "UbuntuServer",
                        Sku = "18.04-LTS",
                        Version = "latest"
                    }
                }
            });

            vmList.Add(vm);
        }

        // Script para instalar Docker e inicializar o Swarm no Manager
        var managerScript = new VirtualMachineExtension("manager-script", new()
        {
            ResourceGroupName = resourceGroup.Name,
            VmName = vmList[0].Name,
            Publisher = "Microsoft.Azure.Extensions",
            Type = "CustomScript",
            TypeHandlerVersion = "2.1",
            Settings = Output.Create(new Dictionary<string, object>
            {
                ["fileUris"] = new[]
                {
                    "https://github.com/manoelvsneto/DockerSwarmAzurePulumi/blob/main/init-swarm.sh"
                },
                ["commandToExecute"] = "bash init-swarm.sh"
            })
        });

        // Você pode criar scripts adicionais para os workers entrarem no Swarm, usando um token recuperado do manager.

        // Export IPs
        for (int i = 0; i < vmNames.Length; i++)
        {
            this.Outputs[$"{vmNames[i]}-ip"] = publicIps[i].IpAddress;
        }
    }
}

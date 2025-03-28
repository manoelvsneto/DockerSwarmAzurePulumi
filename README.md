# 🐳 Docker Swarm Cluster on Azure with Pulumi (C#)

This repository contains a Pulumi program written in C# that provisions a **Docker Swarm cluster** on Microsoft Azure using three Linux virtual machines (1 manager and 2 workers).

## 📦 Project Structure

- `Program.cs` — Main Pulumi stack code that:
  - Creates an Azure Resource Group, Virtual Network, and Subnet.
  - Provisions 3 VMs (Ubuntu 18.04).
  - Sets up SSH access using your local public key.
  - Runs a `CustomScriptExtension` to initialize Docker and the Swarm on the manager node.

## 🛠 Requirements

- [.NET SDK 6+](https://dotnet.microsoft.com/en-us/)
- [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- SSH key pair (`~/.ssh/id_rsa` and `~/.ssh/id_rsa.pub`)
- Git

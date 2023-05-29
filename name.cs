using System;
using Microsoft.Win32;
using System.Management;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Principal;

public static class TelaPrincipal
{
    [STAThread]
    public static void Main()
    {
        VerificarNome();
    }

    private static void VerificarNome()
    {
        string NomePC = Environment.MachineName;
        if (NomePC == "SERVIDORSQL") {
            MessageBox.Show("O computador já está corretamente nomeado como SERVIDORSQL.");
            Environment.Exit(0);
        }
        else { RenomearPC(); }

    }

    private static bool CheckExecutadoComoADM()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void ExecutarComoADM()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.UseShellExecute = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
        startInfo.Verb = "runas";

        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao reiniciar o aplicativo com privilégios de administrador: " + ex.Message);
        }
    }

    private static void RenomearPC()
    {
        if (!CheckExecutadoComoADM())
        {
            ExecutarComoADM();
            Environment.Exit(0);
        }

        string novoNome = "SERVIDORSQL";

        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", true))
            {
                if (key != null)
                {
                    key.SetValue("ComputerName", novoNome);
                    key.SetValue("ActiveComputerName", novoNome);
                }
                else
                {
                    MessageBox.Show("Erro ao acessar o registro do sistema.");
                    return;
                }
            }
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    ManagementBaseObject inParams = obj.GetMethodParameters("Rename");
                    inParams["Name"] = novoNome;
                    ManagementBaseObject outParams = obj.InvokeMethod("Rename", inParams, null);
                    uint resultCode = (uint)outParams["ReturnValue"];

                    if (resultCode == 0){
                        MessageBox.Show("O computador foi renomeado com sucesso. Agora vai estar sendo reiniciado...");
                        Process.Start("shutdown", "/r /t 0");
                    }
                    else{MessageBox.Show("Ocorreu um erro ao tentar renomear o computador.");}
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu uma exceção: {ex.Message}");
        }
    }

}
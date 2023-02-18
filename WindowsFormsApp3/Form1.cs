using System;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Configuration;

namespace CheckFailedProfile
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ExitWindowsEx(int uFlags, int dwReason);
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }



        private void button1_Click(object sender, EventArgs e)
        {
            Form1.ActiveForm.Close();
        }
        
        private void send_message(string text)
        {
            int port = Convert.ToInt32(ConfigurationSettings.AppSettings["port"]);
            string recipients= ConfigurationSettings.AppSettings["recipients"];
            string sender= ConfigurationSettings.AppSettings["sender"];
            string mailserver= ConfigurationSettings.AppSettings["mailserver"];
            string recipient = recipients;
            string subject = "TSFarm profile problem";
            string email = sender;
            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient(mailserver)
                {
                    Port = port,
                };
                smtpClient.Send(email, recipient, subject, text);
            }
            catch
            {
                richTextBox2.Text += "\n\n\n\nНе уадлось отправить сообщение.";
            }
        }

        private string powershellRun(string sid)
        {
            PowerShell powerShellInstance = PowerShell.Create();
            powerShellInstance.AddScript("Get-Disk | select location | where {$_.Location -notmatch \"Integrated\" -AND $_.location -notmatch \"SCSI0\"}");
            Collection<PSObject> psOutput = powerShellInstance.Invoke();
            bool profileFound = false;
            string result = null;
            foreach (PSObject outputItem in psOutput)
            {
                if (outputItem != null)
                {
                    string disklocation = outputItem.Properties["location"].Value.ToString();
                    if (disklocation.Contains(sid))
                    {
                        profileFound = true;
                        if (disklocation.IndexOf(sid + "_ro.vhdx", 0, StringComparison.OrdinalIgnoreCase) != -1 ||
                    disklocation.IndexOf("c:\\windows\\temp", 0, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            result = disklocation;
                        }
                        else
                        {
                            result = null;
                        }
                        break;
                    }
                }
            }
            if (!profileFound)
            {
                result = "Профиль не найден";
            }
            return result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExitWindowsEx(0, 0);
        }

        public void ResultMessage(string userName, string sid, string message, string time)
        {
            string text= "Сведения для администраторов:" +
            "\nИмя пользователя: " + userName +
            "\nSID: " + sid +
            "\nСообщение: " + message +
            "\nИмя компьютера: " + Environment.MachineName.ToString() +
            "\nВремя:" + time;
            richTextBox2.Text = text;
            send_message(text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
            string logType = "Microsoft-FSLogix-Apps/Operational";
            string query = "*[System[(EventID=51) and Security[@UserID='" + sid + "'] " +
                "and TimeCreated[timediff(@SystemTime) <= 120000]]]";
            string query1 = "*[System[(EventID=26) " +
                "and TimeCreated[timediff(@SystemTime) <= 120000]]]";
            string time = null;
            string message = null;

            try
            {
                var elQuery = new EventLogQuery(logType, PathType.LogName, query);
                var elReader = new EventLogReader(elQuery);
                for (EventRecord eventInstance = elReader.ReadEvent(); eventInstance != null; eventInstance = elReader.ReadEvent())
                {
                    if (eventInstance.FormatDescription() != null) {
                        message = eventInstance.FormatDescription();
                        time = eventInstance.TimeCreated.ToString();
                        ResultMessage(userName, sid, message, time);
                        break;
                    }
                }
                if (message == null)
                {
                    message = powershellRun(sid);
                    if (message != null)
                    {
                        try
                        {
                            var elQuery1 = new EventLogQuery(logType, PathType.LogName, query1);
                            var elReader1 = new EventLogReader(elQuery1);
                            for (EventRecord eventInstance = elReader1.ReadEvent();
                                eventInstance != null; eventInstance = elReader1.ReadEvent())
                            {
                                if (eventInstance.FormatDescription() != null)
                                {
                                    if (eventInstance.FormatDescription().IndexOf(sid, 0, StringComparison.OrdinalIgnoreCase) != -1)
                                    {
                                        message += "\n" + eventInstance.FormatDescription();
                                        time = eventInstance.TimeCreated.ToString();
                                        break;
                                    }

                                }
                            }
                        }
                        catch { }

                        if (time == null)
                        {
                            time = DateTime.Now.ToString();
                        }

                        ResultMessage(userName, sid, message, time);
                    }
                    else
                    {
                        this.Close();
                    }
                }
            } 
            catch
            {
                message = powershellRun(sid);
                if (message != null)
                {
                    try
                    {
                        var elQuery1 = new EventLogQuery(logType, PathType.LogName, query1);
                        var elReader1 = new EventLogReader(elQuery1);
                        for (EventRecord eventInstance = elReader1.ReadEvent();
                            eventInstance != null; eventInstance = elReader1.ReadEvent())
                        {
                            if (eventInstance.FormatDescription() != null)
                            {
                                if (eventInstance.FormatDescription().IndexOf(sid, 0, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    message += "\n" + eventInstance.FormatDescription();
                                    time = eventInstance.TimeCreated.ToString();
                                    break;
                                }

                            }
                        }
                    }
                    catch { }
                    if (time == null)
                    {
                        time = DateTime.Now.ToString();
                    }

                    ResultMessage(userName, sid, message, time);
                }
                else
                {
                    this.Close();
                }


            }
        }  
    }
}

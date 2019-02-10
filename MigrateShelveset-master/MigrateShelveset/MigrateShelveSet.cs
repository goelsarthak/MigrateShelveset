using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace MigrateShelveset
{
    public partial class MigrateShelveSet : Form
    {
        #region Variables

        string localFilePath = string.Empty;
        string userName = string.Empty;
        string shelveSet = string.Empty;
        string tfsSource = string.Empty;
        string tfsDestination = string.Empty;

        #endregion

        public MigrateShelveSet()
        {
            InitializeComponent();
        }

        private void MigrateShelveSet_Load(object sender, EventArgs e)
        {
            string serverName = @"https://tfsnbs.gmd.lab/tfs/nbs";

            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(serverName));
            VersionControlServer vcs = tfs.GetService<VersionControlServer>();

            this.DisplayAllBranches(vcs);

            this.GetUserName(tfs);

            tooltipInfo.SetToolTip(cmbUser,"Select the user name who has created the shelveset");
            tooltipInfo.SetToolTip(btnBrowse, "Select the local path where you have mapped the target TFS branch");
            tooltipInfo.SetToolTip(cmbSource, "Select the source TFS branch path");
            tooltipInfo.SetToolTip(cmbDestination, "Select the source TFS branch path");
            tooltipInfo.SetToolTip(cmbShelveSet, "Select the name of the shelveset that you want to migrate");
            tooltipInfo.SetToolTip(btnUnshelve, "Click here to unshelve the shelveset");
            tooltipInfo.SetToolTip(btnHelp, "Click here to know get help");
        }

        private void cmbSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<string, string> selectPair = (KeyValuePair<string, string>)cmbSource.SelectedItem;
            tfsSource = string.Empty;
            tfsSource = selectPair.Value;
        }

        private void cmbDestination_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<string, string> selectPair = (KeyValuePair<string, string>)cmbDestination.SelectedItem;
            tfsDestination = string.Empty;
            tfsDestination = selectPair.Value;
        }

        private void cmbUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            string serverName = @"https://tfsnbs.gmd.lab/tfs/nbs";

            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(serverName));
            VersionControlServer vcs = tfs.GetService<VersionControlServer>();
            KeyValuePair<string, string> selectPair = (KeyValuePair<string, string>)cmbUser.SelectedItem;
            if (cmbUser.SelectedIndex > -1)
            {
                userName = selectPair.Value;
                this.GetShelveSetDetails(vcs);
            }
        }

        private void cmbShelveSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<string, string> selectPair = (KeyValuePair<string, string>)cmbShelveSet.SelectedItem;
            shelveSet = string.Empty;
            shelveSet = selectPair.Value;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.txtFileTextBox.Text = this.folderBrowserDialog1.SelectedPath;
            }
            localFilePath = txtFileTextBox.Text;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.ClearOperation();
        }

        private void btnUnshelve_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(shelveSet) && !string.IsNullOrEmpty(tfsSource) && !string.IsNullOrEmpty(tfsDestination) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(localFilePath))
            {
                string shelvedetails = string.Format("\"{0}\"",shelveSet);
                string command = @"tfpt unshelve" + " " + shelvedetails + ";" + userName + " " + "/nobackup" + " " + "/migrate" + " " + "/source:" + tfsSource + " " + "/target:" + tfsDestination;
                var pp = new ProcessStartInfo("cmd.exe", "/C" + command)
                {
                    UseShellExecute = false,
                    WorkingDirectory = localFilePath,
                };
                var process = Process.Start(pp);
                process.Close();

                this.Refresh();
            }
            else
            {
                MessageBox.Show("Please specify all the details","Error");
            }
        }

        #region Public Methods

        public void GetShelveSetDetails(VersionControlServer vcs)
        {
            BindingList<KeyValuePair<string, string>> shelvedata = new BindingList<KeyValuePair<string, string>>();

            var shelve = vcs.QueryShelvesets(null, userName);
            foreach (var shelveName in shelve)
            {
                string[] shelveSet = shelveName.DisplayName.Split(';');
                shelvedata.Add(new KeyValuePair<string, string>("p1", shelveSet[0]));
            }

            cmbShelveSet.DataSource = new BindingSource(shelvedata, null);
            cmbShelveSet.DisplayMember = "Value";
            cmbShelveSet.ValueMember = "Key";
        }

        public void GetUserName(TfsTeamProjectCollection tfs)
        {
            BindingList<KeyValuePair<string, string>> userdata = new BindingList<KeyValuePair<string, string>>();
            IGroupSecurityService gss = (IGroupSecurityService)tfs.GetService(typeof(IGroupSecurityService));
            Identity SIDS = gss.ReadIdentity(SearchFactor.AccountName, "Project Collection Valid Users", QueryMembership.Expanded);
            Identity[] UserId = gss.ReadIdentities(SearchFactor.Sid, SIDS.Members, QueryMembership.None);

            foreach (Identity user in UserId)
            {
                if (user == null)
                {
                    continue;
                }
                else if (!user.SecurityGroup)
                {
                    userdata.Add(new KeyValuePair<string, string>("q1", user.Domain + @"\" + user.AccountName));
                }
            }

            cmbUser.DataSource = null;
            cmbUser.Items.Clear();

            cmbUser.DataSource = new BindingSource(userdata, null);
            cmbUser.DisplayMember = "Value";
            cmbUser.ValueMember = "Key";
        }

        public void DisplayAllBranches(VersionControlServer vcs)
        {
            BindingList<KeyValuePair<string, string>> source = new BindingList<KeyValuePair<string, string>>();

            BranchObject[] bos = vcs.QueryRootBranchObjects(RecursionType.Full);
            foreach (BranchObject bo in bos)
            {
                for (int tabcounter = 0; tabcounter <= 0; tabcounter++)
                {
                    //if (bo.Properties.RootItem.Item.StartsWith(@"$/PP/Development/ICSDev"))
                    //{
                    //    BranchObject[] childBos = vcs.QueryBranchObjects(bo.Properties.RootItem, RecursionType.OneLevel);
                    //    source.Add(new KeyValuePair<string, string>("r1", bo.Properties.RootItem.Item));
                    //}
                    BranchObject[] childBos = vcs.QueryBranchObjects(bo.Properties.RootItem, RecursionType.OneLevel);
                    source.Add(new KeyValuePair<string, string>("r1", bo.Properties.RootItem.Item));
                }
            }

            //Clearing TFS Source Branch
            cmbSource.DataSource = null;
            cmbSource.Items.Clear();

            //Binding data to TFS Source Branch
            cmbSource.DataSource = new BindingSource(source, null);
            cmbSource.DisplayMember = "Value";
            cmbSource.ValueMember = "Key";

            //Clearing TFS Destination Branch
            cmbDestination.DataSource = null;
            cmbDestination.Items.Clear();

            //Binding data to TFS Destination Branch
            cmbDestination.DataSource = new BindingSource(source, null);
            cmbDestination.DisplayMember = "Value";
            cmbDestination.ValueMember = "Key";
        }

        public void ClearOperation()
        {
            tfsDestination = string.Empty;
            tfsSource = string.Empty;
            userName = string.Empty;
            localFilePath = string.Empty;
            shelveSet = string.Empty;

            txtFileTextBox.Text = string.Empty;

            cmbSource.SelectedIndex = 0;
            cmbUser.SelectedIndex = 0;
            cmbDestination.SelectedIndex = 0;
            cmbUser.SelectedIndex = 0;
        }
        
        #endregion

        private void btnHelp_Click(object sender, EventArgs e)
        {
            Help hlp = new Help();
            hlp.Show();
        }
    }
}

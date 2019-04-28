﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Depressurizer.AutoCats;
using Depressurizer.Core.Enums;
using Rallion;

namespace Depressurizer
{
    public partial class DlgAutoCat : Form
    {
        #region Fields

        public List<AutoCat> AutoCatList;

        private readonly AutoCat initial;

        // public List<Filter> FilterList;
        private readonly GameList ownedGames;

        private AutoCat current;

        private AutoCatConfigPanel currentConfigPanel;

        private string profilePath;

        #endregion

        #region Constructors and Destructors

        public DlgAutoCat(List<AutoCat> autoCats, GameList ownedGames, AutoCat selected, string profile)
        {
            InitializeComponent();

            AutoCatList = new List<AutoCat>();

            profilePath = profile;

            foreach (AutoCat c in autoCats)
            {
                AutoCat clone = c.Clone();
                AutoCatList.Add(clone);
                if (c.Equals(selected))
                {
                    initial = clone;
                }
            }

            this.ownedGames = ownedGames;
        }

        #endregion

        #region Methods

        private void btnDown_Click(object sender, EventArgs e)
        {
            Utility.MoveItem(lstAutoCats, 1);
            RepositionAutoCats();
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            Utility.MoveItem(lstAutoCats, -1);
            RepositionAutoCats();
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFilter.Checked)
            {
                cboFilter.Enabled = true;
                FillFilterList();
            }
            else
            {
                cboFilter.Enabled = false;
            }
        }

        private void cmdCreate_Click(object sender, EventArgs e)
        {
            CreateNewAutoCat();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstAutoCats.SelectedIndex;
            RemoveAutoCat(lstAutoCats.SelectedItem as AutoCat);
            // Select previous item after deleting.
            if (lstAutoCats.Items.Count > 0)
            {
                lstAutoCats.SelectedItem = selectedIndex > 0 ? lstAutoCats.Items[selectedIndex - 1] : lstAutoCats.Items[selectedIndex];
            }
        }

        private void cmdRename_Click(object sender, EventArgs e)
        {
            RenameAutoCat(lstAutoCats.SelectedItem as AutoCat);
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            SaveToAutoCat();
        }

        private void CreateNewAutoCat()
        {
            string name = string.Empty;
            AutoCatType t = AutoCatType.None;
            bool good = true;
            DialogResult result;
            do
            {
                using (DlgAutoCatCreate dialog = new DlgAutoCatCreate())
                {
                    result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        continue;
                    }

                    good = true;
                    name = dialog.SelectedName;
                    t = dialog.SelectedType;
                }

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show(GlobalStrings.DlgAutoCat_MustHaveName);
                    good = false;
                }
                else if (NameExists(name))
                {
                    MessageBox.Show(GlobalStrings.DlgAutoCat_NameInUse);
                    good = false;
                }
                else if (t == AutoCatType.None)
                {
                    MessageBox.Show(GlobalStrings.DlgAutoCat_SelectValidType);
                    good = false;
                }
            } while (result == DialogResult.OK && !good);

            AutoCat newAutoCat = null;
            if (result == DialogResult.OK)
            {
                newAutoCat = AutoCat.Create(t, name);
                if (newAutoCat != null)
                {
                    AutoCatList.Add(newAutoCat);
                }
            }

            AutoCatList.Sort();
            FillAutocatList();
            if (newAutoCat != null)
            {
                lstAutoCats.SelectedItem = newAutoCat;
            }
        }

        private void DlgAutoCat_Load(object sender, EventArgs e)
        {
            FillAutocatList();
            RecreateConfigPanel();
            FillFilterList();

            if (initial != null)
            {
                lstAutoCats.SelectedItem = initial;
            }
        }

        private void FillAutocatList()
        {
            lstAutoCats.Items.Clear();
            foreach (AutoCat ac in AutoCatList)
            {
                lstAutoCats.Items.Add(ac);
            }

            lstAutoCats.DisplayMember = "DisplayName";
        }

        private void FillConfigPanel()
        {
            if (current != null && currentConfigPanel != null)
            {
                currentConfigPanel.LoadFromAutoCat(current);
                if (current.Filter != null)
                {
                    chkFilter.Checked = true;
                    cboFilter.Text = current.Filter;
                }
                else
                {
                    chkFilter.Checked = false;
                }
            }
        }

        private void FillFilterList()
        {
            cboFilter.DataSource = null;
            cboFilter.DataSource = ownedGames.Filters;
            cboFilter.ValueMember = null;
            cboFilter.DisplayMember = "Name";
            cboFilter.Text = string.Empty;
        }

        private void lstAutoCats_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (current != null)
            {
                SaveToAutoCat();
            }

            current = lstAutoCats.SelectedItem as AutoCat;
            RecreateConfigPanel();
            FillConfigPanel();

            if (lstAutoCats.SelectedItem != null)
            {
                btnUp.Enabled = lstAutoCats.SelectedIndex != 0;
                btnDown.Enabled = lstAutoCats.SelectedIndex != lstAutoCats.Items.Count - 1;
            }
            else
            {
                btnUp.Enabled = false;
                btnDown.Enabled = false;
            }
        }

        private bool NameExists(string name)
        {
            foreach (AutoCat ac in AutoCatList)
            {
                if (ac.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private void RecreateConfigPanel()
        {
            if (currentConfigPanel != null)
            {
                panelAutocat.Controls.Remove(currentConfigPanel);
            }

            if (current != null)
            {
                currentConfigPanel = AutoCatConfigPanel.CreatePanel(current, ownedGames, AutoCatList);
            }

            if (currentConfigPanel != null)
            {
                currentConfigPanel.Dock = DockStyle.Fill;
                panelAutocat.Controls.Add(currentConfigPanel);
            }
        }

        private void RemoveAutoCat(AutoCat ac)
        {
            if (ac == null)
            {
                return;
            }

            lstAutoCats.Items.Remove(ac);
            AutoCatList.Remove(ac);
        }

        private void RenameAutoCat(AutoCat ac)
        {
            if (ac == null)
            {
                return;
            }

            bool good = true;
            DialogResult res;
            string name;

            do
            {
                GetStringDlg dlg = new GetStringDlg(ac.Name, GlobalStrings.DlgAutoCat_RenameBoxTitle, GlobalStrings.DlgAutoCat_RenameBoxLabel, GlobalStrings.DlgAutoCat_RenameBoxButton);
                res = dlg.ShowDialog();
                name = dlg.Value;
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show(GlobalStrings.DlgAutoCat_MustHaveName);
                    good = false;
                }
                else if (NameExists(name))
                {
                    MessageBox.Show(GlobalStrings.DlgAutoCat_NameInUse);
                    good = false;
                }
            } while (res == DialogResult.OK && !good);

            if (res == DialogResult.OK)
            {
                ac.Name = name;
            }

            AutoCatList.Sort();
            FillAutocatList();
        }

        private void RepositionAutoCats()
        {
            AutoCatList.Clear();
            foreach (AutoCat ac in lstAutoCats.Items)
            {
                AutoCatList.Add(ac);
            }
        }

        private void SaveToAutoCat()
        {
            if (current != null && currentConfigPanel != null)
            {
                currentConfigPanel.SaveToAutoCat(current);
                if (chkFilter.Checked && cboFilter.Text != string.Empty)
                {
                    current.Filter = cboFilter.Text;
                }
                else
                {
                    current.Filter = null;
                }
            }
        }

        #endregion
    }
}

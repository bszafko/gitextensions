﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Windows.Forms;
using GitCommands;
using ICSharpCode.TextEditor.Document;

namespace GitUI
{
    public partial class FormFileHistory : GitExtensionsForm
    {
        public FormFileHistory(string fileName):base()
        {
            if (fileName.StartsWith(Settings.WorkingDir, StringComparison.InvariantCultureIgnoreCase))
                fileName = fileName.Substring(Settings.WorkingDir.Length);


            this.FileName = fileName;

            InitializeComponent();

            commitInfo.Visible = false;

            Diff.ExtraDiffArgumentsChanged += new EventHandler<EventArgs>(Diff_ExtraDiffArgumentsChanged);
            
            if (GitCommands.Settings.FollowRenamesInFileHistory)
                FileChanges.Filter = " --name-only --follow -- \"" + fileName + "\"";
            else
                FileChanges.Filter = " -- \"" + fileName + "\"";
            FileChanges.SelectionChanged +=new EventHandler(FileChanges_SelectionChanged);
            FileChanges.DisableContextMenu();

            BlameFile.LineViewerStyle = ICSharpCode.TextEditor.Document.LineViewerStyle.FullRow;

            BlameCommitter.ActiveTextAreaControl.VScrollBar.Width = 0;
            BlameCommitter.ActiveTextAreaControl.VScrollBar.Visible = false;
            BlameCommitter.ShowLineNumbers = false;
            BlameCommitter.LineViewerStyle = ICSharpCode.TextEditor.Document.LineViewerStyle.FullRow;
            BlameCommitter.Enabled = false;
            BlameCommitter.ActiveTextAreaControl.TextArea.Dock = DockStyle.Fill;//.Width = BlameCommitter.ActiveTextAreaControl.Width;

            BlameFile.ActiveTextAreaControl.VScrollBar.ValueChanged += new EventHandler(VScrollBar_ValueChanged);
            BlameFile.KeyDown += new KeyEventHandler(BlameFile_KeyUp);
            BlameFile.ActiveTextAreaControl.TextArea.Click += new EventHandler(BlameFile_Click);
            BlameFile.ActiveTextAreaControl.TextArea.KeyDown += new KeyEventHandler(BlameFile_KeyUp);
            BlameFile.ActiveTextAreaControl.KeyDown += new KeyEventHandler(BlameFile_KeyUp);
            BlameFile.ActiveTextAreaControl.TextArea.DoubleClick += new EventHandler(ActiveTextAreaControl_DoubleClick);

            BlameFile.ActiveTextAreaControl.TextArea.MouseMove += new MouseEventHandler(TextArea_MouseMove);
            BlameFile.ActiveTextAreaControl.TextArea.MouseDown += new MouseEventHandler(TextArea_MouseDown);
            BlameFile.ActiveTextAreaControl.TextArea.MouseLeave += new EventHandler(BlameFile_MouseLeave);
            BlameFile.ActiveTextAreaControl.TextArea.MouseEnter += new EventHandler(TextArea_MouseEnter);

            commitInfo.Location = new Point(5, BlameFile.Height - commitInfo.Height - 5);
        }

        void TextArea_MouseEnter(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lastRevision))
                commitInfo.Visible = true;
        }

        string lastRevision;
        void TextArea_MouseDown(object sender, MouseEventArgs e)
        {
            if (blameList != null && BlameFile.ActiveTextAreaControl.TextArea.TextView.GetLogicalLine(e.Y) >= blameList.Count)
                return;

            commitInfo.Visible = true;

            string newRevision = blameList[BlameFile.ActiveTextAreaControl.TextArea.TextView.GetLogicalLine(e.Y)].CommitGuid;
            if (lastRevision != newRevision)
            {
                lastRevision = newRevision;
                commitInfo.SetRevision(lastRevision);
            }
        }

        void TextArea_MouseMove(object sender, MouseEventArgs e)
        {
            commitInfo.Size = new Size(BlameFile.Width - 10, BlameFile.Height / 4);

            if (e.Y > (BlameFile.Height / 3)*2)
            {
                commitInfo.Location = new Point(5, 5);
            } else
            if (e.Y < BlameFile.Height / 3)
            {
                commitInfo.Location = new Point(5, BlameFile.Height - commitInfo.Height - 5);
            }
        }

        void BlameFile_MouseLeave(object sender, EventArgs e)
        {
            commitInfo.Visible = false;
        }


        void Diff_ExtraDiffArgumentsChanged(object sender, EventArgs e)
        {
            UpdateSelectedFileViewers();
        }

        public string FileName { get; set; }

        private void FormFileHistory_Load(object sender, EventArgs e)
        {
            this.Text = "File History (" + FileName + ")";
        }

        private void FileChanges_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void FileChanges_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectedFileViewers();
        }

        private void UpdateSelectedFileViewers()
        {
            List<GitRevision> selectedRows = FileChanges.GetRevisions();

            if (selectedRows.Count == 0) return;

            IGitItem revision = selectedRows[0];

            string fileName = revision.Name;

            if (string.IsNullOrEmpty(fileName))
                fileName = FileName;

            this.Text = "File History (" + fileName + ")";

            if (tabControl1.SelectedTab == Blame)
            {
                //BlameGrid.DataSource = GitCommands.GitCommands.Blame(FileName, revision.CommitGuid);

                int scrollpos = BlameFile.ActiveTextAreaControl.VScrollBar.Value;
                FillBlameTab(revision.Guid, fileName);
                BlameFile.ActiveTextAreaControl.VScrollBar.Value = scrollpos;
            }
            if (tabControl1.SelectedTab == ViewTab)
            {
                int scrollpos = View.ScrollPos;

                View.ViewGitItemRevision(fileName, revision.Guid);
                View.ScrollPos = scrollpos;
            }

            if (selectedRows.Count == 2)
            {
                IGitItem revision1 = selectedRows[0];
                IGitItem revision2 = selectedRows[1];

                if (tabControl1.SelectedTab == DiffTab)
                {
                    Diff.ViewPatch(() => GitCommands.GitCommands.GetSingleDiff(revision1.Guid, revision2.Guid, fileName, Diff.GetExtraDiffArguments()).Text);
                }
            }
            else
                if (selectedRows.Count == 1)
                {
                    IGitItem revision1 = selectedRows[0];

                    if (tabControl1.SelectedTab == DiffTab)
                    {
                        Diff.ViewPatch(() => GitCommands.GitCommands.GetSingleDiff(revision1.Guid, revision1.Guid + "^", fileName, Diff.GetExtraDiffArguments()).Text);
                    }
                }
                else
                {
                    Diff.ViewPatch("You need to select 2 files to view diff.");
                }
        }

        private List<GitBlame> blameList;
        private void FillBlameTab(string guid, string fileName)
        {
            StringBuilder blameCommitter = new StringBuilder();
            StringBuilder blameFile = new StringBuilder();

            blameList = GitCommands.GitCommands.Blame(fileName, guid);

            foreach (GitBlame blame in blameList)
            {
                blameCommitter.AppendLine(blame.Author);
                blameFile.AppendLine(blame.Text);
            }

            EditorOptions.SetSyntax(BlameFile, fileName);

            BlameCommitter.Text = blameCommitter.ToString();
            BlameFile.Text = blameFile.ToString();
        }

        FindAndReplaceForm findAndReplaceForm = new FindAndReplaceForm();

        void BlameFile_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.F)
                findAndReplaceForm.ShowFor(BlameFile, false);
            SyncBlameViews();
        }


        void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            SyncBlameViews();
        }

        void BlameFile_Click(object sender, EventArgs e)
        {
            SyncBlameViews();
        }
        
        private void SyncBlameViews()
        {
            BlameCommitter.ActiveTextAreaControl.VScrollBar.Value = BlameFile.ActiveTextAreaControl.VScrollBar.Value;
            //BlameCommitter.ActiveTextAreaControl.Caret.Line = BlameFile.ActiveTextAreaControl.Caret.Line;
        }

        void ActiveTextAreaControl_DoubleClick(object sender, EventArgs e)
        {
            if (blameList == null || blameList.Count < BlameFile.ActiveTextAreaControl.TextArea.Caret.Line)
                return;

            FormDiffSmall frm = new FormDiffSmall();
            frm.SetRevision(blameList[BlameFile.ActiveTextAreaControl.TextArea.Caret.Line].CommitGuid);
            frm.ShowDialog();
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            FileChanges_SelectionChanged(sender, e);
        }

        private void FileChanges_DoubleClick(object sender, EventArgs e)
        {
            if (FileChanges.GetRevisions().Count > 0)
            {
                IGitItem revision = FileChanges.GetRevisions()[0];

                FormDiffSmall form = new FormDiffSmall();
                form.SetRevision(revision.Guid);
                form.ShowDialog();
            }
            else
                GitUICommands.Instance.StartCompareRevisionsDialog();

        }

        private void FormFileHistory_Shown(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            //EditorOptions.SetSyntax(View, FileName);

            //FileChanges.DataSource = GitCommands.GitCommands.GetFileChanges(FileName);
        }
    }
}

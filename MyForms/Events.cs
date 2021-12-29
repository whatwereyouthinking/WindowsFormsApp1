﻿using System;
using System.Windows.Forms;
using System.IO;

namespace MyForms
{
    public partial class Form1 : Form
    {
        private void OpenDirectoryStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
                TreeView1_Load(dialog.SelectedPath);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // if (e.KeyCode == Keys.Escape)
            switch (e.KeyCode)
            {
                case Keys.OemQuestion:
                    if (e.Modifiers == Keys.Control)
                        searchBox1.Focus();

                    break;
            }
        }

        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ListView1_Load(e.Node);
        }

        private void TreeView1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                case Keys.Space:
                    if (treeView1.SelectedNode is TreeNode n)
                        ListView1_Load(n);

                    // link: https://stackoverflow.com/questions/13952932/disable-beep-of-enter-and-escape-key-c-sharp
                    // retrieved: 2021_12_13
                    e.Handled = e.SuppressKeyPress = true;
                    break;
                case Keys.Up:
                    if (e.KeyData.HasFlag(Keys.Alt))
                    {
                        var parent = _currentDirectory.Parent;
                        TreeView1_Load(parent.FullName);
                        ListView1_Load(parent);
                    }

                    break;
            }
        }

        private void ListView1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    SetSelectedDirectoryTree();
                    break;
                case Keys.Up:
                    if (e.KeyData.HasFlag(Keys.Alt))
                    {
                        var parent = _currentDirectory.Parent;
                        TreeView1_Load(parent.FullName);
                        ListView1_Load(parent);
                    }
                    break;
            }
        }

        private void ListView1_DoubleClick(object sender, System.EventArgs e)
        {
            SetSelectedDirectoryTree();
        }

        private void ListView1_SelectedIndexChanged_UsingItems(object sender, System.EventArgs e)
        {
            ClearPreviewPane();

            if (listView1.SelectedItems.Count == 0)
                return;

            var indices = listView1.SelectedIndices;
            var lastIndex = indices[indices.Count - 1];
            var modelItem = _listViewItems[lastIndex];

            var fullName = modelItem.FullName;
            var extension = modelItem.Extension;
            bool isDirectory = modelItem.Attributes.HasFlag(FileAttributes.Directory);

            if (!isDirectory)
            {
                switch (extension.ToUpper())
                {
                    case ".PDF":
                        SetPreviewPane(NewPdfPreview(fullName));
                        break;
                    default:
                        SetPreviewPane(NewPlainTextPreview(fullName));
                        break;
                }
            }
        }

        private async void SearchBox_TextChangedAsync(object sender, EventArgs e)
        {
            _searchBoxChanged = Forms.NewCancellationSource(_searchBoxChanged);
            SearchResultsPanel.Controls.Clear();
            await ChangeSearchResultsAsync(_searchBoxChanged.Token);
        }

        private async void TagButton_DoubleClickAsync(object sender, EventArgs e)
        {
            _searchBoxChanged = Forms.NewCancellationSource(_searchBoxChanged);
            SearchResultsPanel.Controls.Clear();
            string text = (sender as Forms.SearchResult)?.Text;

            await Forms.AddListLayoutAsync(
                parent: SearchResultsPanel,
                list: _database.GetNamesMatchingTag(text),
                onDoubleClick: DocumentButton_DoubleClickAsync,
                myCancellationToken: _searchBoxChanged.Token,
                labelText: $"Documents with the tag \"{text}\":"
            );
        }

        private async void DocumentButton_DoubleClickAsync(object sender, EventArgs e)
        {
            _searchBoxChanged = Forms.NewCancellationSource(_searchBoxChanged);
            SearchResultsPanel.Controls.Clear();
            string text = (sender as Forms.SearchResult)?.Text;

            await Forms.AddListLayoutAsync(
                parent: SearchResultsPanel,
                list: _database.GetTagsMatchingName(text),
                onDoubleClick: TagButton_DoubleClickAsync,
                myCancellationToken: _searchBoxChanged.Token,
                labelText: $"Tags for the document \"{text}\":"
            );
        }
    }
}

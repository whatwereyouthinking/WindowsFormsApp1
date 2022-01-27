﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;

namespace MyForms
{
    public partial class Form1 : Form
    {
        public enum LayoutType : int
        {
            Search,
            Select,
        }

        internal class LayoutDictionary :
            Dictionary<LayoutType, MasterPane> { }

        private readonly IDataContext _database;
        private readonly PreviewPane _myPreviewPane;
        private readonly TreeViewPane _myTreeViewPane;
        private readonly ListViewPane _myListViewPane;
        private readonly LayoutDictionary _mainLayouts;
        private CancellationTokenSource _searchBoxChanged;

        private void InitializeMyComponent()
        {
            this.selectValueLayoutPanel1.LayoutChanged += new System.EventHandler(this.SelectValuePane_LayoutChanged);
        }

        public Form1(IDataContext myDatabase, string startingDirectory)
        {
            InitializeComponent();
            InitializeMyComponent();

            _database = myDatabase;
            _myPreviewPane = new PreviewPane(splitContainer2.Panel2);
            _myTreeViewPane = new TreeViewPane(treeView1, startingDirectory);
            _myListViewPane = new ListViewPane(listView1, startingDirectory);
            _mainLayouts = new LayoutDictionary
            {
                [LayoutType.Search] = searchResultLayoutPanel1,
                [LayoutType.Select] = selectValueLayoutPanel1
            };

            MainPanels[LayoutType.Select].Clear();
        }

        internal PreviewPane MyPreviewPane
        {
            get => _myPreviewPane;
        }

        internal TreeViewPane MyTreeViewPane
        {
            get => _myTreeViewPane;
        }

        internal ListViewPane MyListViewPane
        {
            get => _myListViewPane;
        }

        internal LayoutDictionary
        MainPanels
        {
            get => _mainLayouts;
        }

        internal CancellationTokenSource SearchBoxChanged
        {
            get => _searchBoxChanged;
            set => _searchBoxChanged = value;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximumSize = new System.Drawing.Size(
                Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height
            );
        }

        private async Task
        ShowMatchingTagResults(
                CancellationToken myCancellationToken,
                string text
            )
        {
            MainPanels[LayoutType.Search].Controls.Clear();

            var myFlowLayoutPanel = new SearchResultLayout(
                parent: MainPanels[LayoutType.Search],
                labelText: $"Documents with the tag \"{text}\":"
            );

            try
            {
                foreach (var item in _database.GetNamesMatchingTag(text))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += DocumentSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += DocumentSearchResult_DoubleClickAsync;
                    await Task.Run(() => myFlowLayoutPanel.Add(mySearchResult));
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task
        ShowMatchingDateResults(
                CancellationToken myCancellationToken,
                string text
            )
        {
            MainPanels[LayoutType.Search].Controls.Clear();

            var myFlowLayoutPanel = new SearchResultLayout(
                parent: MainPanels[LayoutType.Search],
                labelText: $"Documents with the date \"{text}\":"
            );

            try
            {
                foreach (var item in _database.GetNamesMatchingDate(text))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += DocumentSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += DocumentSearchResult_DoubleClickAsync;
                    await Task.Run(() => myFlowLayoutPanel.Add(mySearchResult));
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task
        ShowMatchingDocumentResults(
                CancellationToken myCancellationToken,
                string text
            )
        {
            MainPanels[LayoutType.Search].Clear();

            var label = ILayout.NewLabel;
            label.Text = $"Document: {text}";

            MainPanels[LayoutType.Search].Controls.Add(label);
            MainPanels[LayoutType.Search].Controls.Add(ILayout.NewSpacing);

            try
            {
                foreach (var item in _database.GetTagsMatchingName(text))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += TagSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += TagSearchResult_DoubleClickAsync;

                    await Task.Run(() =>
                        MainPanels[LayoutType.Search]
                            .AddInOrder<SearchResultLayout>(
                                key: MasterPane.SublayoutType.Tags,
                                mySearchResult: mySearchResult
                            )
                    );
                }

                foreach (var item in _database.GetDatesMatchingName(text))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += DateSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += DateSearchResult_DoubleClickAsync;

                    await Task.Run(() =>
                        MainPanels[LayoutType.Search]
                            .AddInOrder<SearchResultLayout>(
                                key: MasterPane.SublayoutType.Dates,
                                mySearchResult: mySearchResult
                            )
                    );
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task
        ChangeResultsAsync(
                CancellationToken myCancellationToken,
                LayoutType mainPanelKey
            )
        {
            MainPanels[mainPanelKey].Clear();
            string text = searchBox1.Text;

            if (text.Length == 0)
                return;

            Match exact = Regex.Match(text, "(?<=^\\s*\").*(?=\"\\s*$)");

            if (exact.Success)
                text = exact.Groups[1].Value;

            try
            {
                foreach (string item in _database.GetNamesMatchingSubstring(text, exact.Success))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += DocumentSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += DocumentSearchResult_DoubleClickAsync;

                    await Task.Run(() =>
                        MainPanels[LayoutType.Search]
                            .AddInOrder<SearchResultLayout>(
                                key: MasterPane.SublayoutType.Documents,
                                mySearchResult: mySearchResult
                            )
                    );
                }

                foreach (string item in _database.GetTagsMatchingSubstring(text, exact.Success))
                {
                    myCancellationToken.ThrowIfCancellationRequested();
                    var mySearchResult = new SearchResult() { Text = item };
                    mySearchResult.Click += TagSearchResult_ClickAsync;
                    mySearchResult.DoubleClick += TagSearchResult_DoubleClickAsync;

                    await Task.Run(() =>
                        MainPanels[LayoutType.Search]
                            .AddInOrder<SearchResultLayout>(
                                key: MasterPane.SublayoutType.Tags,
                                mySearchResult: mySearchResult
                            )
                    );
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task
        SetSelectedDirectoryTreeAsync()
        {
            var modelItem = MyListViewPane.GetLastSelectedItem();
            bool isDirectory = (bool)(modelItem?.Attributes.HasFlag(FileAttributes.Directory));

            if (isDirectory)
            {
                await Task.Run(() => MyTreeViewPane.Load((DirectoryInfo)modelItem));
                await Task.Run(() => MyListViewPane.Load((DirectoryInfo)modelItem));
            }
        }

        private void
        SetValues()
        {
            var mainPanel = MainPanels[LayoutType.Select];
            var documents = mainPanel.GetValues(MasterPane.SublayoutType.Documents);

            if (documents == null)
                return;

            var tags = mainPanel.GetValues(MasterPane.SublayoutType.Tags);

            if (tags != null)
                _database.SetTags(documents, tags);

            var dates = mainPanel.GetValues(MasterPane.SublayoutType.Dates);

            if (dates != null)
                _database.SetDates(documents, dates, Application.DateText.DATE_FORMAT);
        }
    }
}


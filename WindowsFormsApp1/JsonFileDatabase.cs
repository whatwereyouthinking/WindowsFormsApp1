﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Application;

namespace WindowsFormsApp1
{
    public class JsonFileDatabase : IDataContext
    {
        private readonly Root _data;

        public JsonFileDatabase(string filePath)
        {
            _data = new NewtonsoftJsonData.Db<Root>(filePath).Data; 

            if (_data == null)
                _data = new Root();
        }

        private Document GetDocumentMatchingName(string name)
        {
            return (
                from document in _data.Documents
                where document.Name == name
                select document
            )
            .FirstOrDefault()
            ;
        }

        private IEnumerable<Document> GetDocumentsMatchingPattern(string pattern)
        {
            return (
                from document in _data.Documents
                where Regex.IsMatch(document.Name, pattern)
                select document
            )
            ;
        }

        private IEnumerable<Document> GetDocumentsMatchingSubstring(string substring)
        {
            return (
                from document in _data.Documents
                where document.Name.Contains(substring)
                select document
            )
            ;
        }

        private IEnumerable<Document> GetDocumentsMatchingTag(string tag)
        {
            return (
                from document in _data.Documents
                where document.Tags.Contains(tag)
                select document
            )
            ;
        }

        private IEnumerable<string> GetTagsMatchingDocument(Document myDocument)
        {
            return myDocument.Tags;
        }

        public IEnumerable<string> GetNamesMatchingPattern(string pattern)
        {
            return (
                from document in _data.Documents
                where Regex.IsMatch(document.Name, pattern)
                select document.Name
            )
            ;
        }

        public IEnumerable<string> GetNamesMatchingSubstring(string substring)
        {
            return (
                from document in _data.Documents
                where document.Name.Contains(substring)
                select document.Name
            )
            ;
        }

        public IEnumerable<string> GetNamesMatchingTag(string tag)
        {
            return (
                from document in _data.Documents
                where document.Tags.Contains(tag)
                select document.Name
            )
            ;
        }

        public IEnumerable<string> GetTagsMatchingPattern(string pattern)
        {
            return (
                from tag in _data.Tags
                where Regex.IsMatch(tag.Name, pattern)
                select tag.Name
            )
            ;
        }

        public IEnumerable<string> GetTagsMatchingSubstring(string substring)
        {
            return (
                from tag in _data.Tags
                where tag.Name.Contains(substring)
                select tag.Name
            )
            ;
        }

        public IEnumerable<string> GetTagsMatchingName(string tag)
        {
            return GetTagsMatchingDocument(GetDocumentMatchingName(tag));
        }

        public void SetTags(IEnumerable<string> documentNames, IEnumerable<string> tagNames)
        {
            var savedDocuments =
                from name in documentNames
                select GetDocumentMatchingName(name);

            var savedDocumentIds =
                from document in savedDocuments
                select document.Id;

            var savedTagNames =
                from tag in _data.Tags
                select tag.Name;

            foreach (var document in savedDocuments)
                foreach (var tag in tagNames)
                    if (!document.Tags.Contains(tag))
                        document.Tags.Add(tag);

            foreach (var name in tagNames)
            {
                if (!savedTagNames.Contains(name))
                    _data.Tags.Add(
                        new Tag { Name = name, }
                    );

                foreach (Tag myTag in (
                    from tag in _data.Tags
                    where tag.Name == name
                    select tag
                ))
                    foreach (var documentId in savedDocumentIds)
                        if (!myTag.DocumentIds.Contains(documentId))
                            myTag.DocumentIds.Add(documentId);
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Infrastructure
{
    public class PersistentConnector :
        IData<Application.Root>,
        MyForms.IDataReader,
        MyForms.IDataWriter
    {
        private readonly Persistent.EncapsulatedContext<Persistent.Context>
        _context;

        public PersistentConnector(Persistent.Context context)
        {
            _context = new Persistent.EncapsulatedContext<Persistent.Context>(context);
        }

        public Application.Root
        Get()
        {
            return _context.Root();
        }

        public void
        Set(Application.Root root)
        {
            _context.Clear();
            Add(root);
        }

        public void
        Add(Application.Root root)
        {
            _context.Add(root.Documents);
            _context.Add(root.Dates);
            _context.Add(root.Tags);
            _context.Push();
            _context.Add(root.DocumentDates);
            _context.Add(root.DocumentTags);
            _context.Push();
        }

        public IEnumerable<string>
        GetNames()
        {
            return _context.Documents(
                predicate: f => true,
                selector: f => f.Name
            );
        }

        public IEnumerable<string>
        GetTags()
        {
            return _context.Tags(
                predicate: f => true,
                selector: f => f.Name
            );
        }

        public IEnumerable<string>
        GetDates()
        {
            return _context.Dates(
                predicate: f => true,
                selector: f => f.DateString
            );
        }

        public IEnumerable<string>
        GetDatesMatchingName(string name, string format)
        {
            return _context.DocumentDates(
                predicate: f => f.Document.Name == name,
                selector: f => f.Date.Value.ToString(format)
            );
        }

        public IEnumerable<string>
        GetNamesMatchingDate(string date, string format, string pattern)
        {
            if (!Regex.IsMatch(date, pattern))
                return new List<string>();

            // Converting to list pulls all items from the database, making them unqueryable
            // from the database end and truncating the entity so that it no longer contains
            // pointers to any associated entities.
            // 
            //     var entity = _context
            //         .DocumentDates(f => true, f => f)
            //         .ToList()
            //         .Where(f => f.Date.Value.ToString(format).Contains(date))
            //         .Select(f => f)
            //         ;
            //         
            //     entity == new DocumentDate()
            //     {
            //         DocumentId = 157,
            //         Document = null,
            //         DateId = 1,
            //         Date = null
            //     };
            //     
            var dateIds = _context
                .Dates(e => true, e => e)
                .ToList()
                .Where(e => e.Value.ToString(format).Contains(date))
                .Select(e => e.Id)
                ;

            return _context
                .DocumentDates(
                    predicate: f => dateIds.Contains(f.DateId),
                    selector: f => f.Document.Name
                );
        }

        public IEnumerable<string>
        GetNamesMatchingPattern(string pattern)
        {
            /*
            return _context.Documents(
                // TODO
                // error:
                //   The LINQ expression could not be translated. Either rewrite the query in a
                //   form that can be translated, or switch to client evaluation explicitly by
                //   inserting a call to either AsEnumerable(), AsAsyncEnumerable(), ToList(), or
                //   ToListAsync(). See https://go.microsoft.com/fwlink/?linkid=2101038 for more
                //   information.
                // link: https://stackoverflow.com/questions/5720987/how-to-simulate-regular-expressions-in-linq-to-sql
                // link: https://www.codeproject.com/Articles/42764/Regular-Expressions-in-MS-SQL-Server-2005-2008
                // link: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ef/language-reference/how-to-call-custom-database-functions?redirectedfrom=MSDN
                // retrieved: 2022_02_19
                predicate: f => Regex.IsMatch(f.Name, pattern),
                selector: f => f.Name
            );
            */

            return _context
                .Documents(e => true, e => e)
                .ToList()
                .Where(e => Regex.IsMatch(e.Name, pattern))
                .Select(e => e.Name)
                ;
        }

        public IEnumerable<string>
        GetNamesMatchingSubstring(string substring, bool exact = true)
        {
            return _context.Documents(
                // link: https://stackoverflow.com/questions/57872910/the-linq-expression-could-not-be-translated-and-will-be-evaluated-locally
                // link: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ef/language-reference/supported-and-unsupported-linq-methods-linq-to-entities?redirectedfrom=MSDN
                // retrieved: 2022_02_05

                predicate: f => exact
                    ? f.Name.Contains(substring)
                    : f.Name.ToLower().Contains(substring.ToLower()),

                selector: f => f.Name
            );
        }

        public IEnumerable<string>
        GetNamesMatchingTag(string tag)
        {
            return _context.DocumentTags(
                predicate: f => f.Tag.Name == tag,
                selector: f => f.Document.Name
            );
        }

        public IEnumerable<string>
        GetTagsMatchingName(string name)
        {
            return _context.DocumentTags(
                predicate: f => f.Document.Name == name,
                selector: f => f.Tag.Name
            );
        }

        public IEnumerable<string>
        GetTagsMatchingPattern(string pattern)
        {
            /*
            return _context.Tags(
                predicate: f => Regex.IsMatch(f.Name, pattern),
                selector: f => f.Name
            );
            */

            return _context
                .Tags(e => true, e => e)
                .ToList()
                .Where(e => Regex.IsMatch(e.Name, pattern))
                .Select(e => e.Name)
                ;
        }

        public IEnumerable<string>
        GetTagsMatchingSubstring(string substring, bool exact = true)
        {
            return _context.Tags(
                // link: https://stackoverflow.com/questions/57872910/the-linq-expression-could-not-be-translated-and-will-be-evaluated-locally
                // link: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ef/language-reference/supported-and-unsupported-linq-methods-linq-to-entities?redirectedfrom=MSDN
                // retrieved: 2022_02_05

                predicate: f => exact
                    ? f.Name.Contains(substring)
                    : f.Name.ToLower().Contains(substring.ToLower()),

                selector: f => f.Name
            );
        }

        public void
        AddDates(IEnumerable<string> names, IEnumerable<string> dates, string format)
        {
            foreach (string dateString in dates)
            {
                var date = _context.Dates(
                    predicate: e => true,
                    selector: e => e
                )
                .ToList()
                .Where(e => e.Value.ToString(format) == dateString)
                .Select(e => e)
                .SingleOrDefault();

                if (date == null)
                {
                    _context.Add(
                        new Persistent.Date()
                        {
                            Id = _context.NextDateId(),
                            Value = DateTime.ParseExact(dateString, format, null),
                        }
                    );
                }
            }

            _context.Push();

            foreach (string name in names)
            {
                var doc = _context.Documents(
                    predicate: e => e.Name == name,
                    selector: e => e
                )
                .SingleOrDefault();

                foreach (string dateString in dates)
                {
                    var date = _context.Dates(
                        predicate: e => true,
                        selector: e => e
                    )
                    .ToList()
                    .Where(e => e.Value.ToString(format) == dateString)
                    .Select(e => e)
                    .SingleOrDefault();

                    var entity = _context.DocumentDates(
                        predicate: e => e.DocumentId == doc.Id && e.DateId == date.Id,
                        selector: e => e
                    )
                    .SingleOrDefault();

                    if (entity == null)
                        _context.Add(
                            new Persistent.DocumentDate()
                            {
                                DocumentId = doc.Id,
                                Document = doc,
                                DateId = date.Id,
                                Date = date,
                            }
                        );
                }
            }

            _context.Push();
        }

        public void
        AddTags(IEnumerable<string> names, IEnumerable<string> tags)
        {
            foreach (string tagName in tags)
            {
                var tag = _context.Tags(
                    predicate: e => e.Name == tagName,
                    selector: e => e
                )
                .SingleOrDefault();

                if (tag == null)
                {
                    _context.Add(
                        new Persistent.Tag()
                        {
                            Id = _context.NextTagId(),
                            Name = tagName,
                        }
                    );
                }
            }

            _context.Push();

            foreach (string name in names)
            {
                var doc = _context.Documents(
                    predicate: e => e.Name == name,
                    selector: e => e
                )
                .SingleOrDefault();

                foreach (string tagName in tags)
                {
                    var tag = _context.Tags(
                        predicate: e => e.Name == tagName,
                        selector: e => e
                    )
                    .SingleOrDefault();

                    var entity = _context.DocumentTags(
                        predicate: e => e.DocumentId == doc.Id && e.TagId == tag.Id,
                        selector: e => e
                    )
                    .SingleOrDefault();

                    if (entity == null)
                        _context.Add(
                            new Persistent.DocumentTag()
                            {
                                DocumentId = doc.Id,
                                Document = doc,
                                TagId = tag.Id,
                                Tag = tag,
                            }
                        );
                }
            }

            _context.Push();
        }

        public void
        RemoveDates(IEnumerable<string> names, IEnumerable<string> dates, string format)
        {
            _context.Remove(
                _context.DocumentDates(
                    predicate: e =>
                        names.Contains(e.Document.Name) &&
                        dates.Contains(e.Date.Value.ToString(format)),
                    selector: e => e
                )
            );

            _context.Push();
        }

        public void
        RemoveTags(IEnumerable<string> names, IEnumerable<string> tags)
        {
            _context.Remove(
                _context.DocumentTags(
                    predicate: e =>
                        names.Contains(e.Document.Name) &&
                        tags.Contains(e.Tag.Name),
                    selector: e => e
                )
            );

            _context.Push();
        }
    }
}


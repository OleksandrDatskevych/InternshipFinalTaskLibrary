using System.Text.Json;
using Library.BookClasses;

namespace Library
{
    public static partial class Menu
    {
        private static void LibMenu()
        {
            Console.WriteLine("Librarian menu.");
            var menuActions = new Dictionary<int, Action>
            {
                { 1, BooksCatalogueMenu },
                { 2, AddBook },
                { 3, DeleteBook },
                { 4, Sorting },
                { 5, Grouping },
                { 6, SubsList },
                { 7, DeleteSub },
                { 8, LogOut }
            };
            var menuComponents = new List<string>()
            {
                "Catalogue",
                "Add book",
                "Delete book",
                "Sorting",
                "Grouping",
                "Subscribers",
                "Delete subscriber",
                "Log out"
            };
            MenuBuilder(menuComponents, menuActions);
        }

        private static void AddBook()
        {
            Console.WriteLine("Enter book category:");
            var category = Console.ReadLine();
            Console.WriteLine("Enter author:");
            var author = Console.ReadLine();
            Console.WriteLine("Enter title:");
            var title = Console.ReadLine();
            var year = new DateTime(ParseWithPrompt("Enter year of printing:"), 1, 1);
            var qty = ParseWithPrompt("Enter quantity of books:");
            var checkUniqueBook = _listOfBooks
                .Where(i => i.Title == title)
                .Where(i => i.Author == author);

            if (!checkUniqueBook.Any())
            {
                Book newBook = new(_listOfBooks.Any() ? _listOfBooks.Last().Id + 1 : 1, category, author, title, year, qty);
                _listOfBooks.Add(newBook);
                var jsonString = JsonSerializer.Serialize(_listOfBooks, JsonOptions);
                File.WriteAllText(BooksFile, jsonString);
                Console.WriteLine("Book successfully added.");
            }
            else
            {
                Console.WriteLine($"There's already book with title {title} by {author} in the library.");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void DeleteBook()
        {
            BooksCatalogue();
            var bookId = ParseWithPrompt("Enter book ID to delete:");
            var idMatch = _listOfBooks.Where(i => i.Id == bookId).ToList();

            if (idMatch.Any())
            {
                if (_rentedBooks.Where(i => i.BookId == bookId).Any())
                {
                    var subsWithBook = _rentedBooks
                        .Where(i => i.BookId == bookId)
                        .Select(i => i.SubscriberId)
                        .ToList();
                    Console.Write($"Book with ID {bookId} cannot be deleted because it was rented by {subsWithBook.Count} users with following IDs:");

                    foreach (var s in subsWithBook)
                    {
                        Console.Write($" {s}");
                    }
                }
                else
                {
                    _listOfBooks.Remove(idMatch[0]);
                    var jsonString = JsonSerializer.Serialize(_listOfBooks, JsonOptions);
                    File.WriteAllText(BooksFile, jsonString);
                    Console.WriteLine("Book successfully deleted");
                }
            }
            else
            {
                Console.WriteLine("This book does not exist");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void Sorting()
        {
            var chosenOrder = ParseWithPrompt(1, 5, "Choose criterion to sort books:\n1. Category\n2. Author\n3. Title\n4. Year\n5. Quantity");
            var chosenCriterion = ParseWithPrompt(1, 2, "Choose sort order:\n1. Ascending\n2. Descending");

            var sortedList = chosenCriterion switch
            {
                1 => chosenOrder switch
                {
                    1 => _listOfBooks.OrderBy(i => i.Category).ToList(),
                    2 => _listOfBooks.OrderBy(i => i.Author).ToList(),
                    3 => _listOfBooks.OrderBy(i => i.Title).ToList(),
                    4 => _listOfBooks.OrderBy(i => i.Year).ToList(),
                    5 => _listOfBooks.OrderBy(i => i.Quantity).ToList()
                },
                2 => chosenOrder switch
                {
                    1 => _listOfBooks.OrderByDescending(i => i.Category).ToList(),
                    2 => _listOfBooks.OrderByDescending(i => i.Author).ToList(),
                    3 => _listOfBooks.OrderByDescending(i => i.Title).ToList(),
                    4 => _listOfBooks.OrderByDescending(i => i.Year).ToList(),
                    5 => _listOfBooks.OrderByDescending(i => i.Quantity).ToList()
                }
            };

            foreach (var b in sortedList)
            {
                b.PrintBookInfo();
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void Grouping()
        {
            var chosenCriterion = ParseWithPrompt(1, 4, "Choose criterion to group books:\n1. Category\n2. Author\n3. Title\n4. Year");

            switch (chosenCriterion)
            {
                case 1:
                    var result = _listOfBooks.GroupBy(i => i.Category).OrderBy(i => i.Key);

                    foreach (var i in result)
                    {
                        TableBuilderHeader("Category", i.Key, new List<string>() { "Title", "Author", "Year", "Quantity" });

                        foreach (var j in i)
                        {
                            TableBuilderRow(j.Title, j.Author, j.Year.Year.ToString(), j.Quantity.ToString());
                        }
                    }

                    break;
                case 2:
                    result = _listOfBooks.GroupBy(i => i.Author).OrderBy(i => i.Key);

                    foreach (var i in result)
                    {
                        TableBuilderHeader("Author", i.Key, new List<string>() { "Title", "Year", "Quantity", "Category" });

                        foreach (var j in i)
                        {
                            TableBuilderRow(j.Title, j.Year.Year.ToString(), j.Quantity.ToString(), j.Category);
                        }
                    }

                    break;
                case 3:
                    result = _listOfBooks.GroupBy(i => i.Title).OrderBy(i => i.Key);

                    foreach (var i in result)
                    {
                        TableBuilderHeader("Title", i.Key, new List<string>() { "Author", "Year", "Quantity", "Category" });

                        foreach (var j in i)
                        {
                            TableBuilderRow(j.Author, j.Year.Year.ToString(), j.Quantity.ToString(), j.Category);
                        }
                    }

                    break;
                case 4:
                    result = _listOfBooks.GroupBy(i => i.Year.Year.ToString()).OrderBy(i => i.Key);

                    foreach (var i in result)
                    {
                        TableBuilderHeader("Year", i.Key, new List<string>() { "Title", "Author", "Quantity", "Category" });

                        foreach (var j in i)
                        {
                            TableBuilderRow(j.Title, j.Author, j.Quantity.ToString(), j.Category);
                        }
                    }

                    break;
            };

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void SubsList()
        {
            if (_subs.Any())
            {
                foreach (var s in _subs)
                {
                    s.PrintSubInfo();
                }
            }
            else
            {
                Console.WriteLine("There are no subscribers in the library.");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void DeleteSub()
        {
            if (_subs.Any())
            {
                foreach (var s in _subs)
                {
                    s.PrintSubInfo();
                }
            }
            else
            {
                Console.WriteLine("There are no subscribers in the library.");
            }

            var userId = ParseWithPrompt("Enter subscriber ID to delete:");
            var subMatch = _subs.Where(i => i.Id == userId).ToList();

            if (subMatch.Any())
            {
                var subCredsMatch = _subsCreds.Where(i => i.Id == userId).ToList();

                if (RentedBooks(false, subMatch[0]).Any())
                {
                    ReturnAll(subMatch[0]);
                }

                _subs.Remove(subMatch[0]);
                _subsCreds.Remove(subCredsMatch[0]);
                var subsJson = JsonSerializer.Serialize(_subs, JsonOptions);
                var subsCredsJson = JsonSerializer.Serialize(_subsCreds, JsonOptions);
                File.WriteAllText(SubsFile, subsJson);
                File.WriteAllText(SubsCredsFile, subsCredsJson);
                Console.WriteLine("Subscriber successfully deleted");
            }
            else
            {
                Console.WriteLine("This user does not exist");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }
    }
}

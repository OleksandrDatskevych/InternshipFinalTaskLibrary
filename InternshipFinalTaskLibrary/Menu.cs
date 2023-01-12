using System.Text.Json;
using System.Text.RegularExpressions;

namespace InternshipFinalTaskLibrary
{
    internal static class Menu
    {
        private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true, AllowTrailingCommas = true };
        private static readonly string pathToSolution = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\");
        
        private static User? LoggedUser { get; set; }

        private static void MenuBuilder(List<string> menuComponents, Dictionary<int,Action> menuActions)
        {
            var actionFound = false;

            do
            {
                try
                {
                    Console.WriteLine("Choose an option:");

                    foreach (var (c,i) in menuComponents.WithIndex())
                    {
                        Console.WriteLine($"{i + 1}: {c}");
                    }

                    var option = int.Parse(Console.ReadLine());
                    actionFound = option > 0 && option <= menuActions.Count;

                    if (actionFound)
                    {
                        menuActions[option]();
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }
                catch (FormatException)
                {
                    Console.Clear();
                    Console.WriteLine($"Input error. Please enter an integer between 1 and {menuActions.Count}.");
                }
                catch (OverflowException)
                {
                    Console.Clear();
                    Console.WriteLine("Input error. Try again.");
                }
            } while (!actionFound);
        }

        public static void Start()
        {
            Console.WriteLine("Welcome to the library!");
            var menuActions = new Dictionary<int, Action>
            {
                { 1, LoginMenu },
                { 2, SubscribeMenu },
                { 3, LogOut }
            };
            var menuComponents = new List<string>() { "Log in", "Subscribe", "Log out" };
            MenuBuilder(menuComponents, menuActions);
        }

        private static void LoginMenu()
        {
            var subsFile = pathToSolution + @"json\followers.json";
            var libsFile = pathToSolution + @"json\librarians.json";
            var subsCredFile = pathToSolution + @"json\followersCredentials.json";
            var libsCredFile = pathToSolution + @"json\librariansCredentials.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), jsonOptions);
            var libs = JsonSerializer.Deserialize<List<Librarian>>(File.ReadAllText(libsFile), jsonOptions);
            var subsCreds = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredFile), jsonOptions);
            var libsCreds = JsonSerializer.Deserialize<List<LibrarianCredentials>>(File.ReadAllText(libsCredFile), jsonOptions);
            var userFound = false;

            do
            {
                Console.Clear();
                Console.WriteLine("Enter login:");
                var login = Console.ReadLine();
                Console.WriteLine("Enter password:");
                var password = PasswordInput();
                var usersCreds = subsCreds.ConvertAll(i => i as Credentials).Union(libsCreds.ConvertAll(i => i as Credentials));
                var userCredsToLogin = usersCreds.Where(i => i.Login == login).Where(i => i.Password == Cipher(password)).ToList();

                if (userCredsToLogin.Any())
                {
                    var users = subs.ConvertAll(i => i as User).Union(libs.ConvertAll(i => i as User)).ToList();
                    var userToLogin = users.Where(i => i.Id == userCredsToLogin[0].Id).ToList();
                    Console.Clear();
                    userFound = true;
                    LoggedUser = userToLogin[0];
                    MainMenu(LoggedUser);
                }
                else
                {
                    Console.WriteLine($"User with login {login} was not found. Try again.");
                    Console.ReadKey();
                }
            } while (!userFound);
        }

        private static void SubscribeMenu()
        {;
            var subsCredsFile = pathToSolution + @"json\followersCredentials.json";
            var libsCredsFile = pathToSolution + @"json\librariansCredentials.json";
            var subsFile = pathToSolution + @"json\followers.json";
            var subsCreds = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredsFile), jsonOptions);
            var libsCreds = JsonSerializer.Deserialize<List<LibrarianCredentials>>(File.ReadAllText(libsCredsFile), jsonOptions);
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), jsonOptions);
            var userCreds = subsCreds.ConvertAll(i => i as Credentials).Union(libsCreds.ConvertAll(i => i as Credentials));
            var login = string.Empty;

            do
            {
                Console.Clear();
                Console.WriteLine("Enter login (5-12 characters):");
                login = Console.ReadLine();

                if (string.IsNullOrEmpty(login) || login.Length < 5)
                {
                    Console.WriteLine("Login is too short. Try again.");
                    Console.ReadKey();
                }
                else if (login.Length > 12)
                {
                    Console.WriteLine("Login is too long. Try again.");
                    Console.ReadKey();
                }
            } while (string.IsNullOrEmpty(login) || login.Length < 5 || login.Length > 12);

            var existingLoginCheck = userCreds.Where(i => i.Login == login);

            if (!existingLoginCheck.Any())
            {
                Console.WriteLine("Enter password:");
                var password = PasswordInput();
                var firstName = string.Empty;

                static bool checkName(string name)
                {
                    var result = string.IsNullOrEmpty(name) || !name.All(char.IsLetter);

                    return result;
                }

                do
                {
                    Console.WriteLine("Enter first name:");
                    firstName = Console.ReadLine();

                    if (checkName(firstName))
                    {
                        Console.WriteLine("First name field must not contain numbers and have at least 1 character.");
                    }
                } while (checkName(firstName));

                var lastName = string.Empty;

                do
                {
                    Console.WriteLine("Enter last name:");
                    lastName = Console.ReadLine();

                    if (checkName(lastName))
                    {
                        Console.WriteLine("Last name field must not contain numbers and have at least 1 character.");
                    }
                } while (checkName(lastName));

                Console.WriteLine("Enter year of birth");
                var year = Console.ReadLine();
                var yearOfBirth = DateTime.Parse("2021-01-01");
                var tryYear = int.TryParse(year, out int yearNumber);

                if (tryYear)
                {
                    yearOfBirth = new DateTime(yearNumber, 1, 1);
                }

                var newSub = new Subscriber(subs.Any() ? subs.Last().Id + 1 : 1, firstName, lastName, yearOfBirth);
                var newCreds = new SubscriberCredentials(subs.Any() ? subs.Last().Id + 1 : 1, login, Cipher(password));
                subs.Add(newSub);
                subsCreds.Add(newCreds);
                var jsonSubsString = JsonSerializer.Serialize(subs, jsonOptions);
                var jsonSubsCredsString = JsonSerializer.Serialize(subsCreds, jsonOptions);
                File.WriteAllText(subsFile, jsonSubsString);
                File.WriteAllText(subsCredsFile, jsonSubsCredsString);
                Console.WriteLine("Subscription successfully completed.");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine($"This login {login} is already taken.");
                Console.ReadKey();
            }

            Console.Clear();
            Start();
        }

        private static void LogOut()
        {
            Console.WriteLine("Logging out...");
            Environment.Exit(0);
        }

        private static void MainMenu(User user)
        {
            Console.WriteLine($"You are logged in as {Regex.Match(user.GetType().ToString(), @"\w*$")}.\n" +
                $"Name: {user.FirstName} {user.LastName}.");
            if (user is Subscriber)
            {
                SubMenu();
            }
            else if (user is Librarian)
            {
                LibMenu();
            }
        }

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
            var menuComponents = new List<string>() { "Catalogue", "Add book", "Delete book", "Sorting", "Grouping", "Subscribers", 
                "Delete subscriber", "Log out" };
            MenuBuilder(menuComponents, menuActions);
        }

        private static void SubMenu()
        {
            Console.WriteLine("Subscriber menu.");
            var menuActions = new Dictionary<int, Action>
            {
                { 1, BooksCatalogueMenu },
                { 2, RentBook },
                { 3, RentedBooksMenu },
                { 4, ReturnBook },
                { 5, CancelSubscription },
                { 6, LogOut }
            };
            var menuComponents = new List<string>() { "Catalogue", "Rent a book", "Rented books", "Return book", "Cancel subscription", "Log out" };
            MenuBuilder(menuComponents, menuActions);
        }

        private static void BooksCatalogue()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);

            foreach (var b in listOfBooks)
            {
                b.PrintBookInfo();
            }
        }

        private static void BooksCatalogueMenu()
        {
            BooksCatalogue();
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void AddBook()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            Console.WriteLine("Enter book category:");
            var category = Console.ReadLine();
            Console.WriteLine("Enter author:");
            var author = Console.ReadLine();
            Console.WriteLine("Enter title:");
            var title = Console.ReadLine();
            Console.WriteLine("Enter year of printing:");
            var year = new DateTime(int.Parse(Console.ReadLine()), 1, 1);
            Console.WriteLine("Enter quantity of books:");
            var qty = int.Parse(Console.ReadLine());
            Book newBook = new(listOfBooks.Last().Id + 1, category, author, title, year, qty);
            listOfBooks.Add(newBook);
            var jsonString = JsonSerializer.Serialize(listOfBooks, jsonOptions);
            File.WriteAllText(booksFile, jsonString);
            Console.WriteLine("Book successfully added.");
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void DeleteBook()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);

            foreach (var b in listOfBooks)
            {
                b.PrintBookInfo();
            }

            Console.WriteLine("Enter book ID to delete:");
            var bookId = int.Parse(Console.ReadLine());
            var idMatch = listOfBooks.Where(i => i.Id == bookId).ToList();

            if (idMatch.Any())
            {
                listOfBooks.Remove(idMatch[0]);
                var jsonString = JsonSerializer.Serialize(listOfBooks, jsonOptions);
                File.WriteAllText(booksFile, jsonString);
                Console.WriteLine("Book successfully deleted");
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
            var booksFile = pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            var wasListSorted = false;

            do
            {
                try
                {
                    Console.WriteLine("Choose criterion to sort books:\n1. Category\n2. Author\n3. Title\n4. Year\n5. Quantity");
                    int chosenOrder = 0;
                    var chosenCriterion = int.Parse(Console.ReadLine());

                    if (chosenCriterion > 0 && chosenCriterion < 6)
                    {
                        Console.WriteLine("Choose sort order:\n1. Ascending\n2. Descending");
                        chosenOrder = int.Parse(Console.ReadLine());

                        if (chosenOrder < 1 || chosenOrder > 2)
                        {
                            Console.WriteLine("Please enter an integer between 1 and 2.");
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Please enter an integer between 1 and 5.");
                        continue;
                    }

                    var sortedList = chosenOrder switch
                    {
                        1 => chosenCriterion switch
                        {
                            1 => listOfBooks.OrderBy(i => i.Category).ToList(),
                            2 => listOfBooks.OrderBy(i => i.Author).ToList(),
                            3 => listOfBooks.OrderBy(i => i.Title).ToList(),
                            4 => listOfBooks.OrderBy(i => i.Year).ToList(),
                            5 => listOfBooks.OrderBy(i => i.Quantity).ToList()
                        },
                        2 => chosenCriterion switch
                        {
                            1 => listOfBooks.OrderByDescending(i => i.Category).ToList(),
                            2 => listOfBooks.OrderByDescending(i => i.Author).ToList(),
                            3 => listOfBooks.OrderByDescending(i => i.Title).ToList(),
                            4 => listOfBooks.OrderByDescending(i => i.Year).ToList(),
                            5 => listOfBooks.OrderByDescending(i => i.Quantity).ToList()
                        }
                    };

                    foreach (var b in sortedList)
                    {
                        b.PrintBookInfo();
                    }

                    wasListSorted = true;
                    Console.ReadKey();
                    Console.Clear();
                    MainMenu(LoggedUser);
                }
                catch (FormatException)
                {
                    Console.WriteLine($"Input error. Please enter an integer.");
                }
                catch (OverflowException)
                {
                    Console.WriteLine("Input error. Integer is too long.");
                }
            } while (!wasListSorted);
        }

        private static void Grouping()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            var wasListGrouped = false;

            do
            {
                try
                {
                    Console.WriteLine("Choose criterion to group books:\n1. Category\n2. Author\n3. Title\n4. Year");
                    var chosenCriterion = int.Parse(Console.ReadLine());

                    if (chosenCriterion < 0 || chosenCriterion > 4)
                    {
                        Console.WriteLine("Please enter an integer between 1 and 4");
                        continue;
                    }

                    switch (chosenCriterion)
                    {
                        case 1:
                            var result = listOfBooks.GroupBy(i => i.Category);

                            foreach (var i in result)
                            {
                                TableBuilderHeader("Category", i.Key, new List<string>() { "Title", "Author", "Year", "Quantity" });

                                foreach (var j in i)
                                {
                                    TableBuilderRow(j.Title, j.Author, j.Year.ToString(), j.Quantity.ToString());
                                }
                            }

                            break;
                        case 2:
                            result = listOfBooks.GroupBy(i => i.Author);

                            foreach (var i in result)
                            {
                                TableBuilderHeader("Author", i.Key, new List<string>() { "Title", "Year", "Quantity", "Category" });

                                foreach (var j in i)
                                {
                                    TableBuilderRow(j.Title, j.Year.ToString(), j.Quantity.ToString(), j.Category);
                                }
                            }

                            break;
                        case 3:
                            result = listOfBooks.GroupBy(i => i.Title);

                            foreach (var i in result)
                            {
                                TableBuilderHeader("Title", i.Key, new List<string>() { "Author", "Year", "Quantity", "Category" });

                                foreach (var j in i)
                                {
                                    TableBuilderRow(j.Author, j.Year.ToString(), j.Quantity.ToString(), j.Category);
                                }
                            }

                            break;
                        case 4:
                            result = listOfBooks.GroupBy(i => i.Year.Year.ToString());

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
                    wasListGrouped = true;
                    MainMenu(LoggedUser);
                }
                catch (FormatException)
                {

                }
                catch (OverflowException)
                {

                }
            } while (!wasListGrouped);
        }

        private static void TableBuilderHeader(string nameOfKey, string key, List<string> fieldNames)
        {
            var header = new string('-', 150);
            var header2 = String.Format("|{0,35}|{1,35}|{2,35}|{3,35}", fieldNames[0], fieldNames[1], fieldNames[2], fieldNames[3]);
            Console.WriteLine($"{nameOfKey}: {key}");
            Console.WriteLine(header);
            Console.WriteLine(header2);
            Console.WriteLine(header);
        }

        private static void TableBuilderRow(string field1, string field2, string field3, string field4)
        {
            var output = String.Format("|{0,35}|{1,35}|{2,35}|{3,35}", field1, field2, field3, field4);
            Console.WriteLine(output);
        }

        private static void SubsList()
        {
            var subsFile = pathToSolution + @"json\followers.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), jsonOptions);

            if (subs.Any())
            {
                foreach (var s in subs)
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
            var subsFile = pathToSolution + @"json\followersCredentials.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), jsonOptions);

            if (subs.Any())
            {
                foreach (var s in subs)
                {
                    s.PrintSubInfo();
                }
            }
            else
            {
                Console.WriteLine("There are no subscribers in the library.");
            }

            Console.WriteLine("Enter subscriber ID to delete:");
            var userId = int.Parse(Console.ReadLine());
            var subMatch = subs.Where(i => i.Id == userId).ToList();

            if (subMatch.Any())
            {
                subs.Remove(subMatch[0]);
                var jsonString = JsonSerializer.Serialize(subs, jsonOptions);
                File.WriteAllText(subsFile, jsonString);
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

        private static void RentBook()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var rentedBooksFile = pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBooks>>(File.ReadAllText(rentedBooksFile), jsonOptions);
            BooksCatalogue();
            Console.WriteLine("Enter book ID:");
            var bookId = int.Parse(Console.ReadLine());
            var foundBook = listOfBooks.Where(i => i.Id == bookId).ToList();

            if (foundBook.Any())
            {
                if (rentedBooks.Any(i => i.SubscriberId == LoggedUser.Id && i.BookId == foundBook[0].Id))
                {
                    Console.WriteLine("This book is already rented");
                }
                else
                {
                    var newRentedBook = new RentedBooks(foundBook[0].Id, LoggedUser.Id);
                    rentedBooks.Add(newRentedBook);
                    var bookIndex = listOfBooks.FindIndex(i => i.Id == foundBook[0].Id);
                    listOfBooks[bookIndex].Quantity--;
                    Console.WriteLine($"Book {foundBook[0].Title} successfully rented");
                }
                var booksJson = JsonSerializer.Serialize(listOfBooks, jsonOptions);
                var rentedBooksJson = JsonSerializer.Serialize(rentedBooks, jsonOptions);
                File.WriteAllText(booksFile, booksJson);
                File.WriteAllText(rentedBooksFile, rentedBooksJson);
            }
            else
            {
                Console.WriteLine("This book does not exist");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void RentedBooks()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var rentedBooksFile = pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBooks>>(File.ReadAllText(rentedBooksFile), jsonOptions);
            var rentedBooksByUser = rentedBooks.Where(i => i.SubscriberId == LoggedUser.Id).ToList();
            var booksOfUser = new List<Book>();

            foreach (var b in listOfBooks)
            {
                foreach (var r in rentedBooksByUser)
                {
                    if (r.BookId == b.Id)
                    {
                        booksOfUser.Add(b);
                    }
                }
            }

            foreach (var b in booksOfUser)
            {
                b.PrintBookInfo();
            }
        }

        private static void RentedBooksMenu()
        {
            RentedBooks();
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void ReturnBook()
        {
            var booksFile = pathToSolution + @"json\books.json";
            var rentedBooksFile = pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBooks>>(File.ReadAllText(rentedBooksFile), jsonOptions);
            var rentedBooksByUser = rentedBooks.Where(i => i.SubscriberId == LoggedUser.Id).ToList();
            var booksOfUser = new List<Book>();

            foreach (var b in listOfBooks)
            {
                foreach (var r in rentedBooksByUser)
                {
                    if (r.BookId == b.Id)
                    {
                        booksOfUser.Add(b);
                    }
                }
            }

            RentedBooks();
            Console.WriteLine("Enter book ID:");
            var bookId = int.Parse(Console.ReadLine());
            var foundBook = booksOfUser.Where(i => i.Id == bookId).ToList();

            if (foundBook.Any())
            {
                var rentedBookIndex = rentedBooks.FindIndex(i => i.BookId == bookId);
                rentedBooks.RemoveAt(rentedBookIndex);
                var bookIndex = listOfBooks.FindIndex(i => i.Id == bookId);
                listOfBooks[bookIndex].Quantity++;
                Console.WriteLine($"Book {listOfBooks[bookIndex].Title} successfully returned");
            }
            else
            {
                Console.WriteLine("This books is not rented");
            }

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void CancelSubscription()
        {
            throw new NotImplementedException();
        }

        private static string Cipher(string str)
        {
            var result = new char[str.Length];
            var key = "_b9-3)bur1b3)bbhl2b4luivt1vv24lhisfy9b3bhoi(";

            for (var i = 0; i < str.Length; i++)
            {
                result[i] = (char)(str[i] ^ key[i % key.Length]);
            }

            return new string(result);
        }

        private static string PasswordInput()
        {
            var password = string.Empty;
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            while (keyInfo.Key != ConsoleKey.Enter)
            {
                if (keyInfo.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += keyInfo.KeyChar;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        password = password.Substring(0, password.Length - 1);
                        var pos = Console.CursorLeft;
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }

                keyInfo = Console.ReadKey(true);
            }

            Console.WriteLine();
            return password;
        }

        private static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}

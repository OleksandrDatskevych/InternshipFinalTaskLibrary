using System.Text.Json;
using System.Text.RegularExpressions;

namespace InternshipFinalTaskLibrary
{
    internal static class Menu
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, AllowTrailingCommas = true };
        private static readonly string _pathToSolution = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\");
        
        private static User? LoggedUser { get; set; }

        public static void Start()
        {
            Console.WriteLine("Welcome to the library!");
            var menuActions = new Dictionary<int, Action>
            {
                { 1, LoginMenu },
                { 2, SubscribeMenu },
                { 3, LogOut }
            };
            var menuComponents = new List<string>()
            { 
                "Log in", 
                "Subscribe", 
                "Log out" 
            };
            MenuBuilder(menuComponents, menuActions);
        }

        private static void MenuBuilder(List<string> menuComponents, Dictionary<int,Action> menuActions)
        {
            var prompt = "Choose an option:";

            foreach (var (c,i) in menuComponents.WithIndex())
            {
                prompt += $"\n{i + 1}. {c}";
            }

            var option = ParseWithPrompt(1, menuActions.Count, prompt);

            if (LoggedUser is not null && LoggedUser is Subscriber)
            {
                if ((LoggedUser as Subscriber).SubTerm < DateTime.Now && menuActions[option] != LogOut)
                {
                    Console.WriteLine("Please renew subscription");
                    RenewSubscription();
                }
            }
            else
            {
                menuActions[option]();
            }
        }

        private static void LoginMenu()
        {
            var subsFile = _pathToSolution + @"json\followers.json";
            var libsFile = _pathToSolution + @"json\librarians.json";
            var subsCredFile = _pathToSolution + @"json\followersCredentials.json";
            var libsCredFile = _pathToSolution + @"json\librariansCredentials.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);
            var libs = JsonSerializer.Deserialize<List<Librarian>>(File.ReadAllText(libsFile), _jsonOptions);
            var subsCreds = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredFile), _jsonOptions);
            var libsCreds = JsonSerializer.Deserialize<List<LibrarianCredentials>>(File.ReadAllText(libsCredFile), _jsonOptions);
            var userFound = false;

            do
            {
                Console.Clear();
                Console.WriteLine("Enter login:");
                var login = Console.ReadLine();
                Console.WriteLine("Enter password:");
                var password = PasswordInput();
                var usersCreds = subsCreds
                    .ConvertAll(i => i as Credentials)
                    .Union(libsCreds.ConvertAll(i => i as Credentials));
                var userCredsToLogin = usersCreds
                    .Where(i => i.Login == login)
                    .Where(i => i.Password == Cipher(password))
                    .ToList();

                if (userCredsToLogin.Any())
                {
                    var users = subs
                        .ConvertAll(i => i as User)
                        .Union(libs.ConvertAll(i => i as User))
                        .ToList();
                    var userToLogin = users
                        .Where(i => i.Id == userCredsToLogin[0].Id)
                        .ToList();
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
            var subsCredsFile = _pathToSolution + @"json\followersCredentials.json";
            var libsCredsFile = _pathToSolution + @"json\librariansCredentials.json";
            var subsFile = _pathToSolution + @"json\followers.json";
            var subsCreds = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredsFile), _jsonOptions);
            var libsCreds = JsonSerializer.Deserialize<List<LibrarianCredentials>>(File.ReadAllText(libsCredsFile), _jsonOptions);
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);
            var userCreds = subsCreds
                .ConvertAll(i => i as Credentials)
                .Union(libsCreds.ConvertAll(i => i as Credentials));
            var login = string.Empty;

            bool IsLoginValid(string l) => !string.IsNullOrEmpty(l) && l.Length >= 5 && l.Length <= 12;
            bool CheckForLetters(string s) => string.IsNullOrEmpty(s) || !s.All(char.IsLetter) ? false : true;

            string NameInput(string nameType)
            {
                var name = string.Empty;
                do
                {
                    Console.WriteLine($"Enter {nameType.ToLower()} name:");
                    name = Console.ReadLine();

                    if (!CheckForLetters(name))
                    {
                        Console.WriteLine($"{nameType} name field must not contain numbers/symbols and must have at least 1 character.");
                    }
                } while (!CheckForLetters(name));

                return name;
            }

            do
            {
                Console.Clear();
                Console.WriteLine("Enter login (5-12 characters):");
                login = Console.ReadLine();

                if (!IsLoginValid(login))
                {
                    Console.WriteLine("Login is invalid. Try again.");
                    Console.ReadKey();
                }
            }
            while (!IsLoginValid(login));

            var existingLoginCheck = userCreds.Where(i => i.Login == login);

            if (!existingLoginCheck.Any())
            {
                Console.WriteLine("Enter password:");
                var password = PasswordInput();
                var firstName = NameInput("First");
                var lastName = NameInput("Last");
                var yearOfBirth = new DateTime(ParseWithPrompt("Enter year of birth"), 1, 1);
                var newSub = new Subscriber(subs.Any() ? subs.Last().Id + 1 : 1, firstName, lastName, yearOfBirth);
                var newCreds = new SubscriberCredentials(subs.Any() ? subs.Last().Id + 1 : 1, login, Cipher(password));
                subs.Add(newSub);
                subsCreds.Add(newCreds);
                var jsonSubsString = JsonSerializer.Serialize(subs, _jsonOptions);
                var jsonSubsCredsString = JsonSerializer.Serialize(subsCreds, _jsonOptions);
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
            Console.WriteLine($"You are logged in as {Regex.Match(user.GetType().ToString(), @"\w*$")}." +
                $"\nName: {user.FirstName} {user.LastName}.");

            switch (user)
            {
                case Subscriber:
                    SubMenu();
                    break;
                case Librarian:
                    LibMenu();
                    break;
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

        private static void SubMenu()
        {
            Console.WriteLine("Subscriber menu.");
            var menuActions = new Dictionary<int, Action>
            {
                { 1, BooksCatalogueMenu },
                { 2, RentBook },
                { 3, RentedBooksMenu },
                { 4, ReturnBookMenu },
                { 5, CancelSubscriptionMenu },
                { 6, LogOut }
            };
            var menuComponents = new List<string>()
            { 
                "Catalogue", 
                "Rent a book", 
                "Rented books", 
                "Return book", 
                "Cancel subscription", 
                "Log out" 
            };
            MenuBuilder(menuComponents, menuActions);
        }

        private static List<Book> BooksCatalogue()
        {
            var booksFile = _pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);

            if (listOfBooks.Any())
            {
                if (LoggedUser is Librarian)
                {
                    Console.WriteLine($"Total number of books: {listOfBooks.Count}");

                    foreach (var b in listOfBooks)
                    {
                        b.PrintBookInfo();
                    }
                }
                else
                {
                    var userAge = (int)(DateTime.Now - (LoggedUser as Subscriber).YearOfBirth).TotalDays / 365;
                    var booksAgeLimit = listOfBooks.Where(i => (int)i.AgeLimit < userAge).ToList();
                    Console.WriteLine($"Total number of books: {booksAgeLimit.Count}");

                    foreach (var b in booksAgeLimit)
                    {
                        b.PrintBookInfo();
                    }
                }
            }
            else
            {
                Console.WriteLine("There is no books in library.");
            }

            return listOfBooks;
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
            var booksFile = _pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            Console.WriteLine("Enter book category:");
            var category = Console.ReadLine();
            Console.WriteLine("Enter author:");
            var author = Console.ReadLine();
            Console.WriteLine("Enter title:");
            var title = Console.ReadLine();
            var year = new DateTime(ParseWithPrompt("Enter year of printing:"), 1, 1);
            var qty = ParseWithPrompt("Enter quantity of books:");
            var checkUniqueBook = listOfBooks
                .Where(i => i.Title == title)
                .Where(i => i.Author == author);

            if (!checkUniqueBook.Any())
            {
                Book newBook = new(listOfBooks.Any() ? listOfBooks.Last().Id + 1 : 1, category, author, title, year, qty);
                listOfBooks.Add(newBook);
                var jsonString = JsonSerializer.Serialize(listOfBooks, _jsonOptions);
                File.WriteAllText(booksFile, jsonString);
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
            var booksFile = _pathToSolution + @"json\books.json";
            var rentedBooksFile = _pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBook>>(File.ReadAllText(rentedBooksFile), _jsonOptions);
            BooksCatalogue();
            var bookId = ParseWithPrompt("Enter book ID to delete:");
            var idMatch = listOfBooks.Where(i => i.Id == bookId).ToList();

            if (idMatch.Any())
            {
                if (rentedBooks.Where(i => i.BookId == bookId).Any())
                {
                    var subsWithBook = rentedBooks
                        .Select(i => i.SubscriberId)
                        .Order()
                        .ToList();
                    Console.Write($"Book with ID {bookId} cannot be deleted because it was rented by {subsWithBook.Count} users with following IDs:");

                    foreach(var s in subsWithBook)
                    {
                        Console.Write($" {s}");
                    }
                }
                else
                {
                    listOfBooks.Remove(idMatch[0]);
                    var jsonString = JsonSerializer.Serialize(listOfBooks, _jsonOptions);
                    File.WriteAllText(booksFile, jsonString);
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
            var booksFile = _pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var chosenOrder = ParseWithPrompt(1, 5, "Choose criterion to sort books:\n1. Category\n2. Author\n3. Title\n4. Year\n5. Quantity");
            var chosenCriterion = ParseWithPrompt(1, 2, "Choose sort order:\n1. Ascending\n2. Descending");

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

            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void Grouping()
        {
            var booksFile = _pathToSolution + @"json\books.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var chosenCriterion = ParseWithPrompt(1, 4, "Choose criterion to group books:\n1. Category\n2. Author\n3. Title\n4. Year");

            switch (chosenCriterion)
            {
                case 1:
                    var result = listOfBooks.GroupBy(i => i.Category).OrderBy(i => i.Key);

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
                    result = listOfBooks.GroupBy(i => i.Author).OrderBy(i => i.Key);

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
                    result = listOfBooks.GroupBy(i => i.Title).OrderBy(i => i.Key);

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
                    result = listOfBooks.GroupBy(i => i.Year.Year.ToString()).OrderBy(i => i.Key);

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

        private static void TableBuilderHeader(string nameOfKey, string key, List<string> fieldNames)
        {
            var separator = new string('-', 165);
            var header = String.Format("|{0,40}|{1,40}|{2,40}|{3,40}", fieldNames[0], fieldNames[1], fieldNames[2], fieldNames[3]);
            Console.WriteLine($"{nameOfKey}: {key}");
            Console.WriteLine(separator);
            Console.WriteLine(header);
            Console.WriteLine(separator);
        }

        private static void TableBuilderRow(string field1, string field2, string field3, string field4)
        {
            var output = String.Format("|{0,40}|{1,40}|{2,40}|{3,40}", field1, field2, field3, field4);
            Console.WriteLine(output);
        }

        private static void SubsList()
        {
            var subsFile = _pathToSolution + @"json\followers.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);

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
            var subsFile = _pathToSolution + @"json\followers.json";
            var subsCredsFile = _pathToSolution + @"json\followersCredentials.json";
            var subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);
            var subsCreds = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredsFile), _jsonOptions);

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

            var userId = ParseWithPrompt("Enter subscriber ID to delete:");
            var subMatch = subs.Where(i => i.Id == userId).ToList();

            if (subMatch.Any())
            {
                var subCredsMatch = subsCreds.Where(i => i.Id == userId).ToList();

                if (RentedBooks(false, subMatch[0]).Any())
                {
                    ReturnAll(subMatch[0]);
                }

                subs.Remove(subMatch[0]);
                subsCreds.Remove(subCredsMatch[0]);
                var subsJson = JsonSerializer.Serialize(subs, _jsonOptions);
                var subsCredsJson = JsonSerializer.Serialize(subsCreds, _jsonOptions);
                File.WriteAllText(subsFile, subsJson);
                File.WriteAllText(subsCredsFile, subsCredsJson);
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
            var booksFile = _pathToSolution + @"json\books.json";
            var rentedBooksFile = _pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBook>>(File.ReadAllText(rentedBooksFile), _jsonOptions);
            BooksCatalogue();
            var bookId = ParseWithPrompt("Enter book ID:");
            var foundBook = listOfBooks.Where(i => i.Id == bookId).ToList();

            if (foundBook.Any())
            {
                if (rentedBooks.Any(i => i.SubscriberId == LoggedUser.Id && i.BookId == foundBook[0].Id))
                {
                    Console.WriteLine("This book is already rented");
                }
                else
                {
                    var newRentedBook = new RentedBook(foundBook[0].Id, LoggedUser.Id);
                    rentedBooks.Add(newRentedBook);
                    var bookIndex = listOfBooks.FindIndex(i => i.Id == foundBook[0].Id);
                    listOfBooks[bookIndex].Quantity--;
                    Console.WriteLine($"Book {foundBook[0].Title} successfully rented");
                    var booksJson = JsonSerializer.Serialize(listOfBooks, _jsonOptions);
                    var rentedBooksJson = JsonSerializer.Serialize(rentedBooks, _jsonOptions);
                    File.WriteAllText(booksFile, booksJson);
                    File.WriteAllText(rentedBooksFile, rentedBooksJson);
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

        private static List<Book> RentedBooks(bool printList, User user)
        {
            var booksFile = _pathToSolution + @"json\books.json";
            var rentedBooksFile = _pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBook>>(File.ReadAllText(rentedBooksFile), _jsonOptions);
            var rentedBooksByUser = rentedBooks.Where(i => i.SubscriberId == user.Id).ToList();
            var booksOfUser = new List<Book>();

            if (rentedBooksByUser.Any())
            {
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

                if (printList)
                {
                    foreach (var b in booksOfUser)
                    {
                        b.PrintBookInfo();
                    }
                }

                return booksOfUser;
            }
            else
            {
                if (printList)
                {
                    Console.WriteLine("You don't have any rented books");
                }

                return booksOfUser;
            }
        }

        private static void RentedBooksMenu()
        {
            RentedBooks(true,LoggedUser);
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void ReturnBook(int bookId, User user)
        {
            var booksFile = _pathToSolution + @"json\books.json";
            var rentedBooksFile = _pathToSolution + @"json\rentedBooks.json";
            var listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(booksFile), _jsonOptions);
            var rentedBooks = JsonSerializer.Deserialize<List<RentedBook>>(File.ReadAllText(rentedBooksFile), _jsonOptions);
            var rentedBooksByUser = rentedBooks.Where(i => i.SubscriberId == user.Id).ToList();
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

            if (booksOfUser.Where(i => i.Id == bookId).Any())
            {
                var rentedBookIndex = rentedBooks.FindIndex(i => i.BookId == bookId);

                if (rentedBookIndex != -1)
                {
                    rentedBooks.RemoveAt(rentedBookIndex);
                }
                else
                {
                    Console.WriteLine("There's no book with such ID");
                }

                var bookIndex = listOfBooks.FindIndex(i => i.Id == bookId);
                listOfBooks[bookIndex].Quantity++;
                Console.WriteLine($"Book {listOfBooks[bookIndex].Title} successfully returned");
                var booksJson = JsonSerializer.Serialize(listOfBooks, _jsonOptions);
                var rentedBooksJson = JsonSerializer.Serialize(rentedBooks, _jsonOptions);
                File.WriteAllText(booksFile, booksJson);
                File.WriteAllText(rentedBooksFile, rentedBooksJson);
            }
            else
            {
                Console.WriteLine("This book is not rented");
            }
        }

        private static void ReturnBookMenu()
        {
            RentedBooks(true,LoggedUser);
            var bookId = ParseWithPrompt("Enter book ID:");
            ReturnBook(bookId,LoggedUser);
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void ReturnAll(User user)
        {
            var booksOfUser = RentedBooks(false, user);

            foreach (var b in booksOfUser)
            {
                ReturnBook(b.Id,user);
            }
        }

        private static void CancelSubscriptionMenu()
        {
            if (RentedBooks(false,LoggedUser).Any())
            {
                Console.WriteLine("Your subscription will be canceled after returning following books:");
                RentedBooks(true,LoggedUser);
                var menuOption = ParseWithPrompt(1, 2, "Choose an option:\n1. Return all\n2. Exit");

                switch (menuOption)
                {
                    case 1:
                        ReturnAll(LoggedUser);
                        Console.WriteLine("All books have been successfully returned");
                        CancelSubscription();
                        Console.WriteLine("Your subscription has been successfully canceled");
                        Console.ReadKey();
                        LogOut();
                        break;
                    case 2:
                        Console.Clear();
                        MainMenu(LoggedUser);
                        break;
                    default:
                        Console.WriteLine("Input error. Press any key to return to main menu...");
                        Console.ReadKey();
                        Console.Clear();
                        MainMenu(LoggedUser);
                        break;
                }
            }
            else
            {
                CancelSubscription();
                Console.WriteLine("Your subscription has been successfully canceled");
                Console.ReadKey();
                LogOut();
            }

            static void CancelSubscription()
            {
                var subsFile = _pathToSolution + @"json\followers.json";
                var subsCredsFile = _pathToSolution + @"json\followersCredentials.json";
                var subsList = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);
                var subsCredsList = JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(subsCredsFile), _jsonOptions);
                var subIndex = subsList.FindIndex(i => i.Id == LoggedUser.Id);
                var subCredsIndex = subsCredsList.FindIndex(i => i.Id == LoggedUser.Id);
                subsList.RemoveAt(subIndex);
                subsCredsList.RemoveAt(subCredsIndex);
                var subsJson = JsonSerializer.Serialize(subsList, _jsonOptions);
                var subsCredsJson = JsonSerializer.Serialize(subsCredsList, _jsonOptions);
                File.WriteAllText(subsFile, subsJson);
                File.WriteAllText(subsCredsFile, subsCredsJson);
            }
        }

        private static void RenewSubscription()
        {
            var subsFile = _pathToSolution + @"json\followers.json";
            var subsList = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(subsFile), _jsonOptions);
            var menuOption = ParseWithPrompt(1, 2, "Do you want to renew subscription?\n1. Yes\n2. No");

            switch (menuOption)
            {
                case 1:
                    var subIndex = subsList.FindIndex(i => i.Id == LoggedUser.Id);
                    subsList[subIndex].SubTerm = subsList[subIndex].SubTerm.AddYears(1);
                    (LoggedUser as Subscriber).SubTerm = (LoggedUser as Subscriber).SubTerm.AddYears(1);
                    var subsJson = JsonSerializer.Serialize(subsList, _jsonOptions);
                    File.WriteAllText(subsFile, subsJson);
                    Console.WriteLine("Your subscription is renewed. Press any key to return to main menu.");
                    Console.ReadKey();
                    Console.Clear();
                    MainMenu(LoggedUser);
                    break;
                case 2:
                    Console.WriteLine("Press any key to return to main menu.");
                    Console.ReadKey();
                    Console.Clear();
                    MainMenu(LoggedUser);
                    break;
                default:
                    Console.WriteLine("Input error. Press any key to return to main menu.");
                    Console.ReadKey();
                    Console.Clear();
                    MainMenu(LoggedUser);
                    break;
            }
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
                        password = password[..^1];
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

        private static int ParseWithPrompt(int startOfRange, int endOfRange, string prompt)
        {
            var result = new int();
            var parsed = false;

            do
            {
                Console.WriteLine(prompt);

                if (int.TryParse(Console.ReadLine(), out int parsedInt))
                {
                    if (parsedInt >= startOfRange && parsedInt <= endOfRange)
                    {
                        result = parsedInt;
                        parsed = true;
                    }
                    else
                    {
                        Console.WriteLine($"Please enter an integer between {startOfRange} and {endOfRange}.");
                    }
                }
                else
                {
                    Console.WriteLine("Unable to parse input. Try again.");
                }
            } while (!parsed);

            return result;
        }

        private static int ParseWithPrompt(string prompt)
        {
            var result = new int();
            var parsed = false;

            do
            {
                Console.WriteLine(prompt);

                if (int.TryParse(Console.ReadLine(), out int parsedInt))
                {
                    result = parsedInt;
                    parsed = true;
                }
                else
                {
                    Console.WriteLine("Unable to parse input. Try again.");
                }
            } while (!parsed);

            return result;
        }

        private static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}

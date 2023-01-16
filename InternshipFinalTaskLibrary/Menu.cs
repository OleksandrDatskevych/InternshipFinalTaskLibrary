using System.Text.Json;
using System.Text.RegularExpressions;
using Library.BookClasses;
using Library.CredentialsClasses;
using Library.UserClasses;

namespace Library
{
    public static partial class Menu
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, AllowTrailingCommas = true };
        private static readonly string PathToSolution = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\");
        private static readonly string SubsFile = PathToSolution + @"json\followers.json";
        private static readonly string LibsFile = PathToSolution + @"json\librarians.json";
        private static readonly string SubsCredsFile = PathToSolution + @"json\followersCredentials.json";
        private static readonly string LibsCredsFile = PathToSolution + @"json\librariansCredentials.json";
        private static readonly string BooksFile = PathToSolution + @"json\books.json";
        private static readonly string RentedBooksFile = PathToSolution + @"json\rentedBooks.json";
        private static List<Subscriber> _subs = JsonSerializer.Deserialize<List<Subscriber>>(File.ReadAllText(SubsFile), JsonOptions);
        private static List<Librarian> _libs = JsonSerializer.Deserialize<List<Librarian>>(File.ReadAllText(LibsFile), JsonOptions);
        private static List<SubscriberCredentials> _subsCreds = 
            JsonSerializer.Deserialize<List<SubscriberCredentials>>(File.ReadAllText(SubsCredsFile), JsonOptions);
        private static List<LibrarianCredentials> _libsCreds = 
            JsonSerializer.Deserialize<List<LibrarianCredentials>>(File.ReadAllText(LibsCredsFile), JsonOptions);
        private static List<Book> _listOfBooks = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(BooksFile), JsonOptions);
        private static List<RentedBook> _rentedBooks = JsonSerializer.Deserialize<List<RentedBook>>(File.ReadAllText(RentedBooksFile), JsonOptions);

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
                else
                {
                    menuActions[option]();
                }
            }
            else
            {
                menuActions[option]();
            }
        }

        private static void LoginMenu()
        {
            var userFound = false;

            do
            {
                Console.Clear();
                Console.WriteLine("Enter login:");
                var login = Console.ReadLine();
                Console.WriteLine("Enter password:");
                var password = PasswordInput();
                var usersCreds = _subsCreds
                    .ConvertAll(i => i as Credentials)
                    .Union(_libsCreds.ConvertAll(i => i as Credentials));
                var userCredsToLogin = usersCreds
                    .Where(i => i.Login == login)
                    .Where(i => i.Password == Cipher(password))
                    .ToList();

                if (userCredsToLogin.Any())
                {
                    var users = _subs
                        .ConvertAll(i => i as User)
                        .Union(_libs.ConvertAll(i => i as User))
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
        {
            var userCreds = _subsCreds
                .ConvertAll(i => i as Credentials)
                .Union(_libsCreds.ConvertAll(i => i as Credentials));
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
                var newSub = new Subscriber(_subs.Any() ? _subs.Last().Id + 1 : 1, firstName, lastName, yearOfBirth);
                var newCreds = new SubscriberCredentials(_subs.Any() ? _subs.Last().Id + 1 : 1, login, Cipher(password));
                _subs.Add(newSub);
                _subsCreds.Add(newCreds);
                var jsonSubsString = JsonSerializer.Serialize(_subs, JsonOptions);
                var jsonSubsCredsString = JsonSerializer.Serialize(_subsCreds, JsonOptions);
                File.WriteAllText(SubsFile, jsonSubsString);
                File.WriteAllText(SubsCredsFile, jsonSubsCredsString);
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

        private static List<Book> BooksCatalogue()
        {
            if (_listOfBooks.Any())
            {
                if (LoggedUser is Librarian)
                {
                    Console.WriteLine($"Total number of books: {_listOfBooks.Count}");

                    foreach (var b in _listOfBooks)
                    {
                        b.PrintBookInfo();
                    }
                }
                else
                {
                    var userAge = (int)(DateTime.Now - (LoggedUser as Subscriber).YearOfBirth).TotalDays / 365;
                    var booksAgeLimit = _listOfBooks.Where(i => (int)i.AgeLimit < userAge).ToList();
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

            return _listOfBooks;
        }

        private static void BooksCatalogueMenu()
        {
            BooksCatalogue();
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
            var output = string.Format("|{0,40}|{1,40}|{2,40}|{3,40}", field1, field2, field3, field4);
            Console.WriteLine(output);
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

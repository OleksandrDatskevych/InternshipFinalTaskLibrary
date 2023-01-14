using System.Text.Json;
using Library.BookClasses;
using Library.UserClasses;

namespace Library
{
    internal static partial class Menu
    {
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

        private static void RentBook()
        {
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

        private static void RentedBooksMenu()
        {
            RentedBooks(true, LoggedUser);
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static List<Book> RentedBooks(bool printList, User user)
        {
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

        private static void ReturnBook(int bookId, User user)
        {
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
            RentedBooks(true, LoggedUser);
            var bookId = ParseWithPrompt("Enter book ID:");
            ReturnBook(bookId, LoggedUser);
            Console.ReadKey();
            Console.Clear();
            MainMenu(LoggedUser);
        }

        private static void ReturnAll(User user)
        {
            var booksOfUser = RentedBooks(false, user);

            foreach (var b in booksOfUser)
            {
                ReturnBook(b.Id, user);
            }
        }

        private static void CancelSubscriptionMenu()
        {
            if (RentedBooks(false, LoggedUser).Any())
            {
                Console.WriteLine("Your subscription will be canceled after returning following books:");
                RentedBooks(true, LoggedUser);
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
                var subIndex = subs.FindIndex(i => i.Id == LoggedUser.Id);
                var subCredsIndex = subsCreds.FindIndex(i => i.Id == LoggedUser.Id);
                subs.RemoveAt(subIndex);
                subsCreds.RemoveAt(subCredsIndex);
                var subsJson = JsonSerializer.Serialize(subs, _jsonOptions);
                var subsCredsJson = JsonSerializer.Serialize(subsCreds, _jsonOptions);
                File.WriteAllText(subsFile, subsJson);
                File.WriteAllText(subsCredsFile, subsCredsJson);
            }
        }

        private static void RenewSubscription()
        {
            var menuOption = ParseWithPrompt(1, 2, "Do you want to renew subscription?\n1. Yes\n2. No");

            switch (menuOption)
            {
                case 1:
                    var subIndex = subs.FindIndex(i => i.Id == LoggedUser.Id);
                    subs[subIndex].SubTerm = subs[subIndex].SubTerm.AddYears(1);
                    (LoggedUser as Subscriber).SubTerm = (LoggedUser as Subscriber).SubTerm.AddYears(1);
                    var subsJson = JsonSerializer.Serialize(subs, _jsonOptions);
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
    }
}

using Library;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Menu.Start();
        }
        catch (Exception ex) when (ex is NullReferenceException ||
                                   ex is ArgumentNullException ||
                                   ex is InvalidOperationException ||
                                   ex is FileNotFoundException ||
                                   ex is DirectoryNotFoundException ||
                                   ex is ArgumentOutOfRangeException ||
                                   ex is IndexOutOfRangeException)
        {
            var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\exceptionsLog.txt");
            var logText = File.ReadAllText(logFile);

            if (new FileInfo(logFile).Length != 0)
            {
                logText += "\n\n";
            }

            logText += $"{DateTime.Now}\n";

            switch (ex)
            {
                case NullReferenceException:
                    logText += "NullReferenceException. There was an attempt to dereference a null object reference.";
                    break;
                case ArgumentNullException:
                    logText += "ArgumentNullException. Method that requires argument was provided no argument.";
                    break;
                case InvalidOperationException:
                    logText += "InvalidOperationException. Method call was invalid for the object's current state.";
                    break;
                case FileNotFoundException:
                    logText += "FileNotFoundException." +
                        "\nCheck if JSON files are located in ..\\..\\..\\json directory and named correctly." +
                        "\nFile for list of books in library - books.json" +
                        "\nFile for list of subscribers - followers.json" +
                        "\nFile for list of librarians - librarians.json" +
                        "\nFiles for users credentials - followersCredentials.json and librariansCredentials.json" +
                        "\nFile for currently rented books - rentedBooks.json";
                    break;
                case DirectoryNotFoundException:
                    logText += "DirectoryNotFoundException. Check if directory with JSON files is located in ..\\..\\..\\ directory";
                    break;
                case ArgumentOutOfRangeException:
                    logText += "ArgumentOutOfRangeException. Value of an argument was outside the allowable range.";
                    break;
                case IndexOutOfRangeException:
                    logText += "IndexOutOfRangeException. There was an attempt to access an element of an array " +
                        "or collection with an index that is outside its bounds.";
                    break;
            }

            logText += $"\n\n{ex.Message}\n{ex.StackTrace}";
            File.WriteAllText(logFile, logText);
        }
    }
}

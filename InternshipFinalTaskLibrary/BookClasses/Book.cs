namespace Library.BookClasses
{
    public class Book
    {
        public enum AgeCategory
        {
            Child = 3,
            Education = 6,
            Poetry = 6,
            Horror = 18
        }

        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Author { get; set; }
        public string? Title { get; set; }
        public DateTime Year { get; set; }
        public int Quantity { get; set; }
        public AgeCategory AgeLimit { get; set; }

        public Book(int id, string category, string author, string title, DateTime year, int quantity)
        {
            Id = id;
            Category = category;
            Author = author;
            Title = title;
            Year = year;
            Quantity = quantity;

            if (Enum.IsDefined(typeof(AgeCategory), category))
            {
                AgeLimit = Enum.Parse<AgeCategory>(category);
            }
            else
            {
                AgeLimit = AgeCategory.Horror;
            }
        }

        public void PrintBookInfo()
        {
            Console.WriteLine($"ID: {Id} - Category: {Category}, \"{Title}\" by {Author}, {Year.Year} year, Quantity: {Quantity}");
        }
    }
}

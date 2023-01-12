namespace InternshipFinalTaskLibrary
{
    internal class Book
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Author { get; set; }
        public string? Title { get; set; }
        public DateTime Year { get; set; }
        public int Quantity { get; set; }

        public Book(int id, string category, string author, string title, DateTime year, int quantity)
        {
            Id = id;
            Category = category;
            Author = author;
            Title = title;
            Year = year;
            Quantity = quantity;
        }

        public void PrintBookInfo()
        {
            Console.WriteLine($"ID: {Id} - Category: {Category}, Author: {Author}, Title: {Title}, Year: {Year}");
        }
    }
}

namespace InternshipFinalTaskLibrary
{
    internal class RentedBooks
    {
        public int BookId { get; set; }
        public int SubscriberId { get; set; }

        public RentedBooks(int bookId, int subscriberId)
        {
            BookId = bookId;
            SubscriberId = subscriberId;
        }
    }
}

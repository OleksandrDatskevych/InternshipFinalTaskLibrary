namespace Library.BookClasses
{
    internal class RentedBook
    {
        public int BookId { get; set; }
        public int SubscriberId { get; set; }

        public RentedBook(int bookId, int subscriberId)
        {
            BookId = bookId;
            SubscriberId = subscriberId;
        }
    }
}

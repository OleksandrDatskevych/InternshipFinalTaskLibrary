namespace Library.UserClasses
{
    internal class Subscriber : User
    {
        public DateTime YearOfBirth { get; set; }
        public DateTime SubTerm { get; set; }

        public Subscriber(int id, string firstName, string lastName, DateTime yearOfBirth) : base(id, firstName, lastName)
        {
            YearOfBirth = yearOfBirth;
            SubTerm = DateTime.Now.AddYears(1);
        }

        public void PrintSubInfo()
        {
            string subCheck() => SubTerm > DateTime.Now ? $"Subscription is valid until {SubTerm}" : "Subscription is not valid";
            Console.WriteLine($"ID: {Id} - {FirstName} {LastName} {YearOfBirth.Year} {subCheck()}");
        }
    }
}

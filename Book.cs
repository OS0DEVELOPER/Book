using System;

namespace BookforSecrete
{
    internal class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }

        public Book(string title, string author, string content)
        {
            Title = title;
            Author = author;
            Content = content;
        }

        public void DisplayInfo()
        {
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Author: {Author}");
            Console.WriteLine($"Content: {Content}");
        }
    }
}

// Models/ViewModels/PostManagementViewModel.cs
using BookingToursWeb.Models;
using System.Collections.Generic;

namespace BookingToursWeb.Models // <-- DÒNG NÀY PHẢI CHÍNH XÁC LÀ "BookingToursWeb.Models"
{
    using System.Collections.Generic;

    public class PostManagementViewModel
    {
        public required IEnumerable<CategoryWithPosts> CategoriesWithPosts { get; set; }
    }

    public class CategoryWithPosts
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public required IEnumerable<Post> Posts { get; set; }
    }
}
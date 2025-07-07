// Models/ViewModels/PostManagementViewModel.cs
using BookingToursWeb.Models;
using System.Collections.Generic;

namespace BookingToursWeb.Models // <-- DÒNG NÀY PHẢI CHÍNH XÁC LÀ "BookingToursWeb.Models"
{
    using System.Collections.Generic;

    public class PostManagementViewModel
    {
        public IEnumerable<CategoryWithPosts> CategoriesWithPosts { get; set; }
    }

    public class CategoryWithPosts
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public IEnumerable<Post> Posts { get; set; }
    }
}
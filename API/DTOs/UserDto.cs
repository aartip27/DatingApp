using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UserDto
    {
        public string username { get; set; }

        public string token { get; set; }
        public string photourl { get; set; }
    }
}
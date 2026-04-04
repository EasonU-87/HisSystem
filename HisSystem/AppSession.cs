using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HisSystem
{
    public static class AppSession
    {
        // 这就是那个“荧光印章”
        // 一旦登录成功，就把那个 User 对象存到这里
        public static User CurrentUser { get; set; } = null;
    }
}

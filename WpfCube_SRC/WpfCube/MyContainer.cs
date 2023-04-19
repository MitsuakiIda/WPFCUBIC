using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace WpfCube
{
    class MyContainer:Container
    {
        protected override object GetService(Type service)
        {
            return base.GetService(service);
        }
        public object MyGetService(Type service)
        {
            return GetService(service);
        }
    }
}

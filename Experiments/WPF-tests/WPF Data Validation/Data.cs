﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace BusinessLayerValidation
{
    public class Person : IDataErrorInfo
    {
        private int age;

        public int Age
        {
            get { return age; }
            set { age = value; }
        }

        public string Error
        {
            get
            {
                return Age %2 == 0?null:"Uneven Personality Detected!";
            }
        }

        public string this[string name]
        {
            get
            {
                string result = null;

                if (name == "Age")
                {
                    if (this.age < 0 || this.age > 150)
                    {
                        result = "Age must not be less than 0 or greater than 150.";
                    }
                }
                return result;
            }
        }
    }
}

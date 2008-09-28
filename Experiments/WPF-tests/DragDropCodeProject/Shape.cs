// copyright Nick Polyak 2008

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DragDropTest
{
    // object behind the List Items (ItemsSource of the list is set to
    // a collection of Shape objects)
    public class Shape
    {

        public Shape() { }

        public Shape(string name, string description) {
            Name = name;
            NumSides = description;
        }

        public string Name { get; set; }

        public string NumSides { get; set; }
    }

    // Collection of Shape objects
    public class Shapes : ObservableCollection<Shape>
    {
        public Shapes()
        {
            Add(new Shape("Circle", "0"));
            Add(new Shape("Triangle", "3"));
            Add(new Shape("Rectangle", "4"));
            Add(new Shape("Pentagon", "5"));
        }
    }
}
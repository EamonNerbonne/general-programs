using System;
using System.Collections.Generic;
using System.Windows;
using LvqLibCli;

namespace LvqGui
{
    public sealed partial class CreateLvqModel
    {
        // ReSharper disable MemberCanBeMadeStatic.Global
        // ReSharper disable MemberCanBePrivate.Global
        public IEnumerable<LvqModelType> ModelTypes => (LvqModelType[])Enum.GetValues(typeof(LvqModelType));
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore MemberCanBeMadeStatic.Global

        public CreateLvqModel() => InitializeComponent();

        void ReseedParam(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedParam();
        void ReseedInst(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedInst();

        void InitializeModel(object sender, RoutedEventArgs e) => ((CreateLvqModelValues)DataContext).ConfirmCreation();
    }
}

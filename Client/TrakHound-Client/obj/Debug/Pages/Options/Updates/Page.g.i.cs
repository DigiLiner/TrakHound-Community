﻿#pragma checksum "..\..\..\..\..\Pages\Options\Updates\Page.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "4405FC73750C8FC9CCB4BEF75CA484A9"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using TH_WPF;
using TrakHound_Client.Pages.Options.Logger;


namespace TrakHound_Client.Pages.Options.Updates {
    
    
    /// <summary>
    /// Page
    /// </summary>
    public partial class Page : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/TrakHound-Client;component/pages/options/updates/page.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\Pages\Options\Updates\Page.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 72 "..\..\..\..\..\Pages\Options\Updates\Page.xaml"
            ((TH_WPF.Button)(target)).Clicked += new TH_WPF.Button.Clicked_Handler(this.CheckForUpdates_Clicked);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 84 "..\..\..\..\..\Pages\Options\Updates\Page.xaml"
            ((TH_WPF.Button)(target)).Clicked += new TH_WPF.Button.Clicked_Handler(this.ApplyUpdates_Clicked);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 152 "..\..\..\..\..\Pages\Options\Updates\Page.xaml"
            ((TH_WPF.Button)(target)).Clicked += new TH_WPF.Button.Clicked_Handler(this.ClearUpdatesQueue_Clicked);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 158 "..\..\..\..\..\Pages\Options\Updates\Page.xaml"
            ((TH_WPF.Button)(target)).Clicked += new TH_WPF.Button.Clicked_Handler(this.RestoreDefaults_Clicked);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}


﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Runtime Version:4.0.30319.17626
//
//     Änderungen an dieser Datei können fehlerhaftes Verhalten verursachen und gehen verloren, wenn
//     der Code wird erneut generiert.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BikeNav.Resources
{
    using System;


    /// <summary>
    ///   Eine stark typisierte Ressourcenklasse zum Suchen von lokalisierten Zeichenfolgen usw.
    /// </summary>
    // Diese Klasse wurde von der StronglyTypedResourceBuilder-Klasse
    // mit einem Tool wie ResGen oder Visual Studio automatisch generiert.
    // Bearbeiten Sie zum Hinzufügen oder Entfernen eines Members die RESX-Datei, und führen Sie dann ResGen
    // mit der Option "/str" erneut aus, oder erstellen Sie das VS-Projekt neu.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class AppResources
    {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal AppResources()
        {
        }

        /// <summary>
        ///   Gibt die von dieser Klasse verwendete zwischengespeicherte ResourceManager-Instanz zurück.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BikeNav.Resources.AppResources", typeof(AppResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Überschreibt die CurrentUICulture-Eigenschaft des aktuellen Threads für alle
        ///   Ressourcenlookups, die diese stark typisierte Ressourcenklasse verwenden.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge wie LeftToRight.
        /// </summary>
        public static string ResourceFlowDirection
        {
            get
            {
                return ResourceManager.GetString("ResourceFlowDirection", resourceCulture);
            }
        }

        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge wie us-EN.
        /// </summary>
        public static string ResourceLanguage
        {
            get
            {
                return ResourceManager.GetString("ResourceLanguage", resourceCulture);
            }
        }

        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge wie MY APPLICATION.
        /// </summary>
        public static string ApplicationTitle
        {
            get
            {
                return ResourceManager.GetString("ApplicationTitle", resourceCulture);
            }
        }

        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge wie Schaltfläche.
        /// </summary>
        public static string AppBarButtonText
        {
            get
            {
                return ResourceManager.GetString("AppBarButtonText", resourceCulture);
            }
        }

        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge wie Menüelement.
        /// </summary>
        public static string AppBarMenuItemText
        {
            get
            {
                return ResourceManager.GetString("AppBarMenuItemText", resourceCulture);
            }
        }
    }
}

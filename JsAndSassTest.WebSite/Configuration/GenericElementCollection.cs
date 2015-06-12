using System;
using System.Configuration;

namespace JsAndSassTest.WebSite.Configuration
{
    /// <summary>
    /// Generic element collection.
    /// </summary>
    public class GenericElementCollection<TElement> : ConfigurationElementCollection where TElement : ConfigurationElement, new()
    {
        /// <summary>
        /// See <see cref="ConfigurationElementCollection.CreateNewElement()"/>.
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return Activator.CreateInstance<TElement>();
        }

        /// <summary>
        /// See <see cref="ConfigurationElementCollection.GetElementKey"/>.
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return KeySelector != null ? KeySelector.Invoke((TElement)element) : new object();
        }

        /// <summary>
        /// Integer indexer.
        /// </summary>
        public TElement this[int index]
        {
            get { return (TElement)BaseGet(index); }
        }

        /// <summary>
        /// Adds the given element to this collection.
        /// </summary>
        public void Add(TElement element)
        {
            BaseAdd(element);
        }

        /// <summary>
        /// Lambda that selects the key property on child elements.
        /// </summary>
        public Func<TElement, object> KeySelector { get; set; }
    }
}
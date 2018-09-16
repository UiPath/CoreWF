// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using CoreWf.DynamicUpdate;
    using Portable.Xaml.Markup;
    using System.Xml.Serialization;

    [ContentProperty("XmlContent")]
    public class DynamicUpdateMapExtension : MarkupExtension
    {
        private NetDataContractXmlSerializable<DynamicUpdateMap> content;

        public DynamicUpdateMapExtension()
        {
        }

        public DynamicUpdateMapExtension(DynamicUpdateMap updateMap)
        {
            this.content = new NetDataContractXmlSerializable<DynamicUpdateMap>(updateMap);
        }

        public DynamicUpdateMap UpdateMap
        {
            get
            {
                return this.content != null ? this.content.Value : null;
            }
        }

        public IXmlSerializable XmlContent
        {
            get
            {
                if (this.content == null)
                {
                    this.content = new NetDataContractXmlSerializable<DynamicUpdateMap>();
                }

                return this.content;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this.UpdateMap;
        }
    }
}

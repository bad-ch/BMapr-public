using System.Xml;
using System.Xml.Serialization;
using OSGeo.GDAL;

namespace BMapr.GDAL.WebApi.Services.schemaWfs110
{


    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Transaction
    {

        private string[] insertField;

        private TransactionUpdate[] updateField;

        private TransactionDelete[] deleteField;

        private string serviceField;

        private string versionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Insert")]
        public string[] Insert
        {
            get
            {
                return this.insertField;
            }
            set
            {
                this.insertField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Update")]
        public TransactionUpdate[] Update
        {
            get
            {
                return this.updateField;
            }
            set
            {
                this.updateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Delete")]
        public TransactionDelete[] Delete
        {
            get
            {
                return this.deleteField;
            }
            set
            {
                this.deleteField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string service
        {
            get
            {
                return this.serviceField;
            }
            set
            {
                this.serviceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionUpdate
    {

        private TransactionUpdateProperty[] propertyField;

        private TransactionUpdateFilter filterField;

        private string typeNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Property")]
        public TransactionUpdateProperty[] Property
        {
            get
            {
                return this.propertyField;
            }
            set
            {
                this.propertyField = value;
            }
        }

        /// <remarks/>
        public TransactionUpdateFilter Filter
        {
            get
            {
                return this.filterField;
            }
            set
            {
                this.filterField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string typeName
        {
            get
            {
                return this.typeNameField;
            }
            set
            {
                this.typeNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionUpdateProperty
    {

        private string nameField;

        [XmlText]
        private string valueField;

        /// <remarks/>
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public object Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                // <<MODIFICATION>>
                var nodes = value as XmlNode[];
                if (nodes != null && nodes.Length == 1)
                {
                    this.valueField = nodes[0].ParentNode.InnerXml;
                    return;
                }

                this.valueField = "";
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionUpdateFilter
    {

        private TransactionUpdateFilterFeatureId featureIdField;

        /// <remarks/>
        public TransactionUpdateFilterFeatureId FeatureId
        {
            get
            {
                return this.featureIdField;
            }
            set
            {
                this.featureIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionUpdateFilterFeatureId
    {

        private string fidField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string fid
        {
            get
            {
                return this.fidField;
            }
            set
            {
                this.fidField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionDelete
    {

        private TransactionDeleteFeatureId[] filterField;

        private string typeNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FeatureId", IsNullable = false)]
        public TransactionDeleteFeatureId[] Filter
        {
            get
            {
                return this.filterField;
            }
            set
            {
                this.filterField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string typeName
        {
            get
            {
                return this.typeNameField;
            }
            set
            {
                this.typeNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TransactionDeleteFeatureId
    {

        private string fidField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string fid
        {
            get
            {
                return this.fidField;
            }
            set
            {
                this.fidField = value;
            }
        }
    }
}

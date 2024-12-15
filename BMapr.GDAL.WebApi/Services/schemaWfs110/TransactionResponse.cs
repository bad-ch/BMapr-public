namespace BMapr.GDAL.WebApi.Services.schemaWfs110
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.opengis.net/wfs", IsNullable = false)]
    public partial class TransactionResponse
    {

        private TransactionResponseTransactionSummary transactionSummaryField;

        private TransactionResponseTransactionResults transactionResultsField;

        private TransactionResponseFeature[] insertResultsField;

        private string versionField;

        /// <remarks/>
        public TransactionResponseTransactionSummary TransactionSummary
        {
            get
            {
                return this.transactionSummaryField;
            }
            set
            {
                this.transactionSummaryField = value;
            }
        }

        /// <remarks/>
        public TransactionResponseTransactionResults TransactionResults
        {
            get
            {
                return this.transactionResultsField;
            }
            set
            {
                this.transactionResultsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Feature", IsNullable = false)]
        public TransactionResponseFeature[] InsertResults
        {
            get
            {
                return this.insertResultsField;
            }
            set
            {
                this.insertResultsField = value;
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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
    public partial class TransactionResponseTransactionSummary
    {

        private int totalInsertedField;

        private int totalUpdatedField;

        private int totalDeletedField;

        /// <remarks/>
        public int totalInserted
        {
            get
            {
                return this.totalInsertedField;
            }
            set
            {
                this.totalInsertedField = value;
            }
        }

        /// <remarks/>
        public int totalUpdated
        {
            get
            {
                return this.totalUpdatedField;
            }
            set
            {
                this.totalUpdatedField = value;
            }
        }

        /// <remarks/>
        public int totalDeleted
        {
            get
            {
                return this.totalDeletedField;
            }
            set
            {
                this.totalDeletedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
    public partial class TransactionResponseTransactionResults
    {

        private TransactionResponseTransactionResultsAction actionField;

        /// <remarks/>
        public TransactionResponseTransactionResultsAction Action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
    public partial class TransactionResponseTransactionResultsAction
    {

        private string messageField;

        private string locatorField;

        private ushort codeField;

        /// <remarks/>
        public string Message
        {
            get
            {
                return this.messageField;
            }
            set
            {
                this.messageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string locator
        {
            get
            {
                return this.locatorField;
            }
            set
            {
                this.locatorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort code
        {
            get
            {
                return this.codeField;
            }
            set
            {
                this.codeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/wfs")]
    public partial class TransactionResponseFeature
    {

        private FeatureId featureIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opengis.net/ogc")]
        public FeatureId FeatureId
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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opengis.net/ogc")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.opengis.net/ogc", IsNullable = false)]
    public partial class FeatureId
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

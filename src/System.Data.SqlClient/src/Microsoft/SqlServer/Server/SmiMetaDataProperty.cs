// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlServer.Server
{
    // SmiMetaDataProperty defines an extended, optional property to be used on the SmiMetaData class
    //  This approach to adding properties is added combat the growing number of sparsely-used properties 
    //  that are specially handled on the base classes



    // Simple collection for properties.  Could extend to IDictionary support if needed in future.
    internal class SmiMetaDataPropertyCollection
    {
        private SmiDefaultFieldsProperty _defaultFields;
        private SmiOrderProperty _sortOrder;
        private SmiUniqueKeyProperty _uniqueKey;
        private bool _isReadOnly;

        // Singleton empty instances to ensure each property is always non-null
        private static readonly SmiDefaultFieldsProperty s_emptyDefaultFields = new SmiDefaultFieldsProperty(null);
        private static readonly SmiOrderProperty s_emptySortOrder = new SmiOrderProperty(null);
        private static readonly SmiUniqueKeyProperty s_emptyUniqueKey = new SmiUniqueKeyProperty(null);

        internal static readonly SmiMetaDataPropertyCollection EmptyInstance = CreateEmptyInstance();

        private static SmiMetaDataPropertyCollection CreateEmptyInstance()
        {
            var emptyInstance = new SmiMetaDataPropertyCollection();
            emptyInstance.SetReadOnly();
            return emptyInstance;
        }

        internal SmiMetaDataPropertyCollection()
        {
       }

        public SmiDefaultFieldsProperty DefaultFields
        {
            get => _defaultFields ?? s_emptyDefaultFields;
            set
            {
                EnsureWritable();
                _defaultFields = value ?? throw ADP.InternalError(ADP.InternalErrorCode.InvalidSmiCall);
            }
        }

        public SmiUniqueKeyProperty UniqueKey
        {
            get => _uniqueKey ?? s_emptyUniqueKey;
            set
            {
                EnsureWritable();
                _uniqueKey = value ?? throw ADP.InternalError(ADP.InternalErrorCode.InvalidSmiCall);
            }
        }

        public SmiOrderProperty SortOrder
        {
            get => _sortOrder ?? s_emptySortOrder;
            set
            {
                EnsureWritable();
                _sortOrder = value ?? throw ADP.InternalError(ADP.InternalErrorCode.InvalidSmiCall);
            }
        }

        internal bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
        }


        // Allow switching to read only, but not back.
        internal void SetReadOnly()
        {
            _isReadOnly = true;
        }

        private void EnsureWritable()
        {
            if (IsReadOnly)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
        }
    }

    // Base class for properties
    internal abstract class SmiMetaDataProperty
    {
    }

    // Property defining a list of column ordinals that define a unique key
    internal class SmiUniqueKeyProperty : SmiMetaDataProperty
    {
        //private IList<bool> _columns;
        private readonly SmiBitArray _columns;

        internal SmiUniqueKeyProperty(SmiBitArray columns)
        {
            _columns = columns;
        }

        // indexed by column ordinal indicating for each column whether it is key or not
        internal bool this[int ordinal]
        {
            get
            {
                if (_columns is null || _columns.Count <= ordinal)
                {
                    return false;
                }
                else
                {
                    return _columns[ordinal];
                }
            }
        }

        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert(( _columns==null || 0 == _columns.Count ) || countToMatch == (_columns?.Count??0),
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _columns?.Count??0 +
                    ") not equal to checked size (" + countToMatch + ")");
        }
    }

    // Property defining a sort order for a set of columns (by ordinal and ASC/DESC).
    internal class SmiOrderProperty : SmiMetaDataProperty
    {
        internal struct SmiColumnOrder
        {
            internal int SortOrdinal;
            internal SortOrder Order;
        }

        private IList<SmiColumnOrder> _columns;

        internal SmiOrderProperty(IList<SmiColumnOrder> columnOrders)
        {
            if (!(columnOrders is null))
            {
                if (columnOrders is ReadOnlyCollection<SmiColumnOrder> readonlyColumnOrders)
                {
                    _columns = readonlyColumnOrders;
                }
                else
                {
                    _columns = new ReadOnlyCollection<SmiColumnOrder>(columnOrders);
                }
            }
        }

        // Readonly list of the columnorder instances making up the sort order
        //  order in list indicates precedence
        internal SmiColumnOrder this[int ordinal]
        {
            get
            {
                if (_columns is null ||_columns.Count <= ordinal)
                {
                    SmiColumnOrder order = new SmiColumnOrder();
                    order.Order = SortOrder.Unspecified;
                    order.SortOrdinal = -1;
                    return order;
                }
                else
                {
                    return _columns[ordinal];
                }
            }
        }


        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert( (_columns is null ||  0 == _columns.Count) || countToMatch == _columns.Count,
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _columns?.Count??0 +
                    ") not equal to checked size (" + countToMatch + ")");
        }
    }

    // property defining inheritance relationship(s)
    internal class SmiDefaultFieldsProperty : SmiMetaDataProperty
    {
        private readonly SmiBitArray _defaults;

        internal SmiDefaultFieldsProperty(SmiBitArray defaultFields)
        {
            _defaults = defaultFields;
        }

        internal bool this[int ordinal]
        {
            get
            {
                if (_defaults is null || _defaults.Count <= ordinal)
                {
                    return false;
                }
                else
                {
                    return _defaults[ordinal];
                }
            }
        }

        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert(( _defaults==null || 0 == _defaults.Count ) || countToMatch == (_defaults?.Count??0),
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _defaults?.Count??0 +
                    ") not equal to checked size (" + countToMatch + ")");
        }
    }
}

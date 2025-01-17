﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Detached.Mappers.TypeOptions
{
    public enum MappingStrategy
    {
        None,
        Primitive,
        Complex,
        Collection,
        Nullable
    }

    public interface ITypeOptions
    {
        Type ClrType { get; }        
        
        Type ItemClrType { get; }

        Dictionary<string, object> Annotations { get; }

        MappingStrategy MappingStrategy { get; }

        bool IsAbstract { get; }

        IEnumerable<string> MemberNames { get; }
 
        IMemberOptions GetMember(string memberName);

        Expression BuildNewExpression(Expression context, Expression discriminator);
    }
}
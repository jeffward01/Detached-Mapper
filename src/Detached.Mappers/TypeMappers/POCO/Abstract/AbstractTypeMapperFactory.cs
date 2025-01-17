﻿using Detached.Mappers.Exceptions;
using Detached.Mappers.TypeOptions;
using Detached.Mappers.TypeOptions.Class;
using System;

namespace Detached.Mappers.TypeMappers.POCO.Abstract
{
    public class AbstractTypeMapperFactory : ITypeMapperFactory
    {
        readonly MapperOptions _options; 

        public AbstractTypeMapperFactory(MapperOptions options)
        {
            _options = options; 
        }

        public bool CanCreate(TypeMapperKey typePair, ITypeOptions sourceType, ITypeOptions targetType)
        {
            return sourceType.IsAbstract() || targetType.IsAbstract();
        }

        public ITypeMapper Create(TypeMapperKey typePair, ITypeOptions sourceType, ITypeOptions targetType)
        {
            Type mapperType = typeof(AbstractTypeMapper<,>).MakeGenericType(typePair.SourceType, typePair.TargetType);

            Type concreteTargetType = targetType.IsAbstract() && !targetType.IsInherited() && targetType.ClrType != typeof(object)
                ? GetConcreteType(targetType.ClrType) 
                : targetType.ClrType;
            
            return (ITypeMapper)Activator.CreateInstance(mapperType, new object[] { _options, typePair.Flags, concreteTargetType });
        }

        public Type GetConcreteType(Type abstractType)
        {
            if (_options.ConcreteTypes.TryGetValue(abstractType, out Type type))
            {
                return type;
            }
            else if (abstractType.IsGenericType && _options.ConcreteTypes.TryGetValue(abstractType.GetGenericTypeDefinition(), out Type genericType))
            {
                return genericType.MakeGenericType(abstractType.GetGenericArguments());
            }
            else
            {
                throw new MapperException($"Can't find a concrete type for abstract type or interface {abstractType}");
            }
        }
    }
}
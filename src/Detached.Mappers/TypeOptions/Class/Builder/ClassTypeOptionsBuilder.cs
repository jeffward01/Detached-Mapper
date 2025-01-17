﻿using Detached.Mappers.Annotations;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Detached.Mappers.TypeOptions.Class.Builder
{
    public class ClassTypeOptionsBuilder<TType>
    {
        public ClassTypeOptionsBuilder(ClassTypeOptions typeOptions, MapperOptions mapperOptions)
        {
            TypeOptions = typeOptions;
            MapperOptions = mapperOptions;
        }

        public ClassTypeOptions TypeOptions { get; }

        public MapperOptions MapperOptions { get; }

        public ClassMemberOptionsBuilder<TType, TMember> Member<TMember>(Expression<Func<TType, TMember>> selector)
        {
            ClassMemberOptions memberOptions = GetMember(selector);

            return new ClassMemberOptionsBuilder<TType, TMember>(TypeOptions, memberOptions, MapperOptions);
        }

        public ClassTypeOptionsBuilder<TType> Constructor(Expression<Func<IMapContext, TType>> constructor)
        {
            TypeOptions.Constructor = constructor;
            return this;
        }

        public ClassTypeOptionsBuilder<TType> IncludeAll()
        {
            foreach (var memberName in TypeOptions.MemberNames)
            {
                TypeOptions.GetMember(memberName).IsNotMapped(false);
            }

            return this;
        }

        public ClassTypeOptionsBuilder<TType> ExcludeAll()
        {
            foreach (var memberName in TypeOptions.MemberNames)
            {
                TypeOptions.GetMember(memberName).IsNotMapped(false);
            }

            return this;
        }

        public ClassTypeOptionsBuilder<TType> IncludePrimitives()
        {
            foreach (var memberName in TypeOptions.MemberNames)
            {
                IMemberOptions memberOptions = TypeOptions.GetMember(memberName);
                if (MapperOptions.IsPrimitive(memberOptions.ClrType))
                {
                    memberOptions.IsNotMapped(false);
                }
                else
                {
                    memberOptions.IsNotMapped(true);
                }
            }

            return this;
        }

        public ClassDiscriminatorBuilder<TType, TMember> Discriminator<TMember>(Expression<Func<TType, TMember>> selector)
        {
            ClassMemberOptions memberOptions = GetMember(selector);
            TypeOptions.SetDiscriminatorName(memberOptions.Name);

            return new ClassDiscriminatorBuilder<TType, TMember>(TypeOptions);
        }

        public ClassTypeOptionsBuilder<TType> Key(params Expression<Func<TType, object>>[] members)
        {
            foreach (string memberName in TypeOptions.MemberNames)
            {
                IMemberOptions memberOptions = TypeOptions.GetMember(memberName);
                memberOptions.IsKey(false);
            }

            foreach (LambdaExpression selector in members)
            {
                var convert = selector.Body as UnaryExpression;
                var member = convert.Operand as MemberExpression;
                var propInfo = member.Member as PropertyInfo;

                if (TypeOptions.Members.TryGetValue(propInfo.Name, out ClassMemberOptions memberOptions))
                {
                    memberOptions.IsKey(true);
                }
            }

            return this;
        }

        ClassMemberOptions GetMember<TMember>(Expression<Func<TType, TMember>> selector)
        {
            string memberName = ((MemberExpression)selector.Body).Member.Name;
            if (!TypeOptions.Members.TryGetValue(memberName, out ClassMemberOptions memberOptions))
                throw new ArgumentException($"Member {memberName} does not exist.");

            return memberOptions;
        }
    }
}
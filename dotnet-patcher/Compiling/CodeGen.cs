#region References
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using Mono.Collections.Generic;
using System.Diagnostics;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
#endregion

namespace DP.Compiling
{
	public static class CodeGen
	{
		#region Methodes
		/// <summary>
		/// Add a using declaration to the specified compilation unit.
		/// </summary>
		/// <param name="cus">The compilation unit to change.</param>
		/// <param name="name">The namespace to add.</param>
		public static void AddUsing(ref CompilationUnitSyntax cus, string name)
		{
			Debug.Assert(cus != null);

			if (string.IsNullOrWhiteSpace(name)) return;

			// If already added, return.
			if (cus.Usings.Count > 0)
			{
				foreach(UsingDirectiveSyntax uds in cus.Usings)
				{
					if (string.CompareOrdinal(uds.Name.ToFullString(), name) == 0)
						return;
				}
			}

			cus = cus.AddUsings(
				SF.UsingDirective(
					SF.ParseName(name)
				)
			);
		}

		/// <summary>
		/// Add attributes to a member declaration.
		/// </summary>
		/// <param name="cca">The attribute list.</param>
		/// <param name="mds">The member declaration to modify.</param>
		/// <param name="cus">The unit where to add using declarations if any.</param>
		public static void AddAttributes(Collection<CustomAttribute> cca, ref MemberDeclarationSyntax mds, ref CompilationUnitSyntax cus)
		{
			Debug.Assert(cus != null);
			Debug.Assert(mds != null);
			Debug.Assert(cca != null);

			if (cca.Count == 0) return;
			AttributeListSyntax attributeList = SF.AttributeList();
			foreach(CustomAttribute ca in cca)
			{
				attributeList = attributeList.AddAttributes(
					SF.Attribute(
						SF.IdentifierName(
							ca.AttributeType.Name
						)
					)
				);

				AddUsing(ref cus, ca.AttributeType.Namespace);
			}
			mds = mds.AddAttributeLists(attributeList);
		}

		/// <summary>
		/// Add a type declaration.
		/// </summary>
		/// <param name="td">The type's definition to add.</param>
		/// <returns>A syntax node corresponding to the added type.</returns>
		private static BaseTypeDeclarationSyntax AddTypeDeclaration(TypeDefinition td)
		{
			if (td.IsClass) return SF.ClassDeclaration(td.Name);
			if (td.IsInterface) return SF.InterfaceDeclaration(td.Name);
			if (td.IsEnum) return SF.EnumDeclaration(td.Name);
			// FIXME: if (td.IsFunctionPointer) return SF.DelegateDeclaration(td.Name);
			return SF.StructDeclaration(td.Name);
		}

		public static void AddInherits(Collection<InterfaceImplementation> parentTypes, ref BaseTypeDeclarationSyntax btds, ref CompilationUnitSyntax cus)
		{
			foreach(InterfaceImplementation parentType in parentTypes)
				AddInherit(parentType.InterfaceType, ref btds, ref cus);
		}

		/// <summary>
		/// Add a parent class or interface to a type declaration.
		/// </summary>
		/// <param name="tr">The type reference to inherith from.</param>
		/// <param name="btds">The syntax node of the type declaration.</param>
		/// <param name="cus">The root node to add using statements on.</param>
		public static void AddInherit(TypeReference tr, ref BaseTypeDeclarationSyntax btds, ref CompilationUnitSyntax cus)
		{
			btds = btds.WithBaseList(
				(btds.BaseList ?? SF.BaseList()).AddTypes(
					SF.SimpleBaseType(
						SF.ParseTypeName(tr.Name)
					)
				)
			);

			AddUsing(ref cus, tr.Namespace);
		}

		public static void AddFields(Collection<FieldDefinition> fields, ref BaseTypeDeclarationSyntax btds, ref CompilationUnitSyntax cus)
		{
			TypeDeclarationSyntax tds = btds as TypeDeclarationSyntax;
			Debug.Assert(tds != null);
			foreach(FieldDefinition field in fields)
				AddField(field, ref tds, ref cus);
			btds = tds;
		}

		public static void AddField(FieldDefinition field, ref TypeDeclarationSyntax type, ref CompilationUnitSyntax cus)
		{
			FieldDeclarationSyntax fd = SF.FieldDeclaration(
				SF.VariableDeclaration(
					SF.ParseTypeName(field.FieldType.Name)
				).AddVariables(
					SF.VariableDeclarator(
						SF.Identifier(
							field.Name
							)
						)
					)
				);

			if (field.IsAssembly) fd = fd.AddModifiers(SF.Token(SyntaxKind.InternalKeyword));
			else if (field.IsPrivate) fd = fd.AddModifiers(SF.Token(SyntaxKind.PrivateKeyword));
			else if (field.IsPublic) fd = fd.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));
			else  fd = fd.AddModifiers(SF.Token(SyntaxKind.ProtectedKeyword));
			if (field.IsStatic) fd = fd.AddModifiers(SF.Token(SyntaxKind.StaticKeyword));

			AddUsing(ref cus, field.FieldType.Namespace);

			type = type.AddMembers(fd);
		}

		/// <summary>
		/// Add the specified type to the compilation unit.
		/// </summary>
		/// <param name="td">The type to add.</param>
		/// <param name="cus">The compilation unit. A new one is created if null is provided.</param>
		public static void AddTypeDefinition(TypeDefinition td, ref CompilationUnitSyntax cus)
		{
			if (cus == null) cus = SF.CompilationUnit();

			BaseTypeDeclarationSyntax type = AddTypeDeclaration(td);
			if (td.IsNotPublic) type = type.AddModifiers(SF.Token(SyntaxKind.PrivateKeyword));
			if (td.IsPublic) type = type.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));
			if (td.IsAbstract) type = type.AddModifiers(SF.Token(SyntaxKind.AbstractKeyword));
			if (td.IsSealed) type = type.AddModifiers(SF.Token(SyntaxKind.SealedKeyword));
			// TODO: static ?

			MemberDeclarationSyntax mds = type as MemberDeclarationSyntax;

			Debug.Assert(mds != null);

			AddAttributes(td.CustomAttributes, ref mds, ref cus);
			AddInherit(td.BaseType, ref type, ref cus);
			AddInherits(td.Interfaces, ref type, ref cus);
			AddFields(td.Fields, ref type, ref cus);

			if (!string.IsNullOrWhiteSpace(td.Namespace))
			{
				NamespaceDeclarationSyntax ns = SF.NamespaceDeclaration(
					SF.IdentifierName(td.Namespace)
				);
				mds = ns.AddMembers(mds);
			}

			if (type != null) cus = cus.AddMembers(type);
		}
		
		/// <summary>
		/// Generate the corresponding CSharp code.
		/// </summary>
		/// <param name="cus">The compilation unit to transform into a source code.</param>
		/// <returns>The corresponding source code.</returns>
		public static string ToString(CompilationUnitSyntax cus)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			using(System.IO.StringWriter sw = new System.IO.StringWriter(sb))
			{
				sw.Write(cus.NormalizeWhitespace().ToFullString());
			}
			return sb.ToString();
		}
		#endregion
	}
}
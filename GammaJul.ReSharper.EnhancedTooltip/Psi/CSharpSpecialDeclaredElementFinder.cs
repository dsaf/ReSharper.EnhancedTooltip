﻿using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace GammaJul.ReSharper.EnhancedTooltip.Psi {

	[Language(typeof(CSharpLanguage))]
	internal sealed class CSharpSpecialDeclaredElementFinder : ISpecialDeclaredElementFinder {

		public DeclaredElementInstance FindDeclaredElement(ITreeNode node, IFile file, out TextRange sourceRange) {
			TokenNodeType tokenType = node.GetTokenType();

			if (tokenType == CSharpTokenType.VAR_KEYWORD)
				return FindTypeFromVarKeyword(node, file, out sourceRange);

			if (tokenType == CSharpTokenType.NEW_KEYWORD)
				return FindTypeFromNewKeyword(node, file, out sourceRange);

			sourceRange = TextRange.InvalidRange;
			return null;
		}

		[CanBeNull]
		private static DeclaredElementInstance FindTypeFromVarKeyword([NotNull] ITreeNode varKeyword, [NotNull] IFile file, out TextRange sourceRange) {
			sourceRange = TextRange.InvalidRange;

			var multipleLocalVariableDeclaration = varKeyword.Parent as IMultipleLocalVariableDeclaration;
			if (multipleLocalVariableDeclaration == null)
				return null;

			IMultipleDeclarationMember member = multipleLocalVariableDeclaration.DeclaratorsEnumerable.FirstOrDefault();
			if (member == null)
				return null;

			var type = member.Type as IDeclaredType;
			if (type == null)
				return null;

			ITypeElement typeElement = type.GetTypeElement();
			if (typeElement == null)
				return null;

			sourceRange = file.GetDocumentRange(varKeyword.GetTreeTextRange()).TextRange;
			return new DeclaredElementInstance(typeElement, type.GetSubstitution());
		}

		[CanBeNull]
		private static DeclaredElementInstance FindTypeFromNewKeyword([NotNull] ITreeNode newKeyword, [NotNull] IFile file, out TextRange sourceRange) {
			sourceRange = TextRange.InvalidRange;

			var creation = newKeyword.Parent as IObjectCreationExpression;
			if (creation == null)
				return null;

			DeclaredElementInstance instance = TryResolveReference(creation.ConstructorReference) ?? TryResolveReference(creation.TypeReference);
			if (instance == null)
				return null;

			sourceRange = file.GetDocumentRange(newKeyword.GetTreeTextRange()).TextRange;
			return instance;
		}

		[CanBeNull]
		private static DeclaredElementInstance TryResolveReference([CanBeNull] IReference reference) {
			if (reference != null) {
				IResolveResult resolveResult = reference.Resolve().Result;
				if (resolveResult.DeclaredElement != null)
					return new DeclaredElementInstance(resolveResult.DeclaredElement, resolveResult.Substitution);
			}
			return null;
		}

	}

}
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using OmmCMS.Core.Search;
using OmmCMS.Core.Search.Facets;

namespace OmmCMS.Core.Tests.Search;

[TestFixture]
public class FacetExtractionTests
{
    [TestCase("categoryid", FacetGroupKind.Category)]
    [TestCase("attrid1", FacetGroupKind.Attribute)]
    [TestCase("forumid", FacetGroupKind.Forum)]
    [TestCase("customerid", FacetGroupKind.Customer)]
    [TestCase("createdon", FacetGroupKind.Date)]
    [TestCase("manufacturerid", FacetGroupKind.Unknown)]
    [TestCase("price", FacetGroupKind.Unknown)]
    [TestCase("deliveryid", FacetGroupKind.Unknown)]
    [TestCase("available", FacetGroupKind.Unknown)]
    [TestCase("availablestart", FacetGroupKind.Unknown)]
    [TestCase("variantid1", FacetGroupKind.Unknown)]
    public void Classifies_content_facets_without_commerce_semantics(string key, FacetGroupKind expected)
    {
        Assert.That(FacetGroup.GetKindByKey("Content", key), Is.EqualTo(expected));
    }

    [Test]
    public void Removes_only_selected_multi_select_value()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/articles";
        context.Request.QueryString = new QueryString("?categoryid=1,2&i=3&query=hello");
        var facet = new Facet("1", new FacetValue(1, IndexTypeCode.Int32));
        _ = new FacetGroup("Content", "categoryid", "Category", true, false, 0, new[] { facet });

        var url = new ContentFacetUrlHelper(context.Request).Remove(facet);
        var query = QueryHelpers.ParseQuery(url.Substring(url.IndexOf('?')));
        Assert.That(query["categoryid"].ToString(), Is.EqualTo("2"));
        Assert.That(query.ContainsKey("i"), Is.False);
        Assert.That(query["query"].ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void Removes_single_selection_even_if_query_uses_alternate_value()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/articles";
        context.Request.QueryString = new QueryString("?categoryid=category-slug&query=hello");
        var facet = new Facet("1", new FacetValue(1, IndexTypeCode.Int32));
        _ = new FacetGroup("Content", "categoryid", "Category", false, false, 0, new[] { facet });

        var url = new ContentFacetUrlHelper(context.Request).Remove(facet);
        var query = QueryHelpers.ParseQuery(url.Substring(url.IndexOf('?')));
        Assert.That(query.ContainsKey("categoryid"), Is.False);
        Assert.That(query["query"].ToString(), Is.EqualTo("hello"));
    }

    private sealed class ContentFacetUrlHelper(HttpRequest request) : FacetUrlHelperBase(request)
    {
        public override int Order => 0;
        public override string Scope => "Content";
        protected override string GetUnmappedQueryName(Facet facet) => facet.FacetGroup.Key;
        protected override Dictionary<string, string> GetQueryParts(Facet facet) => new()
        {
            [facet.FacetGroup.Key] = facet.Key
        };
    }
}

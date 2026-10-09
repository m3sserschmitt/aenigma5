/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Resources.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Enigma5.App.Tests.Resources.Handlers;

public class CommandResultExtensionsTests
{
    private static void AssertStatus500(IResult response)
    {
        var problem = Assert.IsType<ProblemHttpResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    #region Checks

    [Fact]
    public void IsSuccessResult_is_true_only_for_a_successful_result()
    {
        Assert.True(CommandResult.CreateResultSuccess<string>().IsSuccessResult());
        Assert.False(CommandResult.CreateResultFailure<string>("value").IsSuccessResult());
        Assert.False(((CommandResult<string>?)null).IsSuccessResult());
    }

    [Fact]
    public void IsSuccessNotNullResultValue_also_needs_a_value()
    {
        Assert.True(CommandResult.CreateResultSuccess("value").IsSuccessNotNullResultValue());
        Assert.False(CommandResult.CreateResultSuccess<string>().IsSuccessNotNullResultValue());
        Assert.False(CommandResult.CreateResultFailure<string>("value").IsSuccessNotNullResultValue());
        Assert.False(((CommandResult<string>?)null).IsSuccessNotNullResultValue());
    }

    #endregion

    #region GET

    [Fact]
    public void CreateGetResponse_answers_200_with_the_value()
    {
        var response = CommandResult.CreateResultSuccess("value").CreateGetResponse();

        Assert.Equal("value", Assert.IsType<Ok<string>>(response).Value);
    }

    [Fact]
    public void CreateGetResponse_answers_200_with_an_empty_list()
    {
        var response = CommandResult.CreateResultSuccess(new List<string>()).CreateGetResponse();

        Assert.Empty(Assert.IsType<Ok<List<string>>>(response).Value!);
    }

    [Fact]
    public void CreateGetResponse_answers_404_when_there_is_no_value()
    {
        Assert.IsType<NotFound>(CommandResult.CreateResultSuccess<string>().CreateGetResponse());
    }

    [Fact]
    public void CreateGetResponse_answers_500_when_the_query_failed()
    {
        AssertStatus500(CommandResult.CreateResultFailure<string>().CreateGetResponse());
        AssertStatus500(((CommandResult<string>?)null).CreateGetResponse());
    }

    #endregion

    #region POST

    [Fact]
    public void CreatePostResponse_answers_200_with_the_value()
    {
        var response = CommandResult.CreateResultSuccess("value").CreatePostResponse();

        Assert.Equal("value", Assert.IsType<Ok<string>>(response).Value);
    }

    [Fact]
    public void CreatePostResponse_answers_500_without_a_value_or_when_the_command_failed()
    {
        AssertStatus500(CommandResult.CreateResultSuccess<string>().CreatePostResponse());
        AssertStatus500(CommandResult.CreateResultFailure<string>().CreatePostResponse());
    }

    #endregion

    #region PUT

    [Fact]
    public void CreatePutResponse_answers_200_when_a_record_was_changed()
    {
        Assert.IsType<Ok>(CommandResult.CreateResultSuccess(1).CreatePutResponse());
    }

    [Fact]
    public void CreatePutResponse_answers_404_when_no_record_was_changed()
    {
        Assert.IsType<NotFound>(CommandResult.CreateResultSuccess(0).CreatePutResponse());
    }

    [Fact]
    public void CreatePutResponse_answers_500_when_the_command_failed()
    {
        AssertStatus500(CommandResult.CreateResultFailure<int>().CreatePutResponse());
        AssertStatus500(((CommandResult<int>?)null).CreatePutResponse());
    }

    #endregion
}

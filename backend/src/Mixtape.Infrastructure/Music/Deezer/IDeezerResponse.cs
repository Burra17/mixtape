namespace Mixtape.Infrastructure.Music.Deezer;

// Varje svar från Deezer kan vara ett fel med status 200, så felet läses på samma sätt för alla anrop.
internal interface IDeezerResponse
{
    DeezerError? Error { get; }
}

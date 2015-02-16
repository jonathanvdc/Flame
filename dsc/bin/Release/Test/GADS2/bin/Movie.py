from IRecord import *

class Movie(IRecord):
    """ Describes a movie. """

    def __init__(self, Id, Title, Rating):
        """ Creates a new movie instance for the given parameters. """
        self.id = Id
        self.title = Title
        self.rating = Rating

    def __str__(self):
        return self.title + " (Rated " + str(self.rating) + ")"

    def __repr__(self):
        return str(self.id) + ": " + str(self)

    @property
    def title(self):
        """ Gets the movie's title. """
        return self.title_value

    @title.setter
    def title(self, value):
        """ Sets the movie's title.
            This accessor is private. """
        self.title_value = value

    @property
    def id(self):
        """ Gets the movie's identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the movie's identifier.
            This accessor is private. """
        self.id_value = value

    @property
    def rating(self):
        """ Gets the movie's rating. """
        return self.rating_value

    @rating.setter
    def rating(self, value):
        """ Sets the movie's rating.
            This accessor is private. """
        self.rating_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id
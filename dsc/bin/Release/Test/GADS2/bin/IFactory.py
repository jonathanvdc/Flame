class IFactory:
    """ A generic interface for the factory pattern.
        It creates instances of type 'T' by using arguments of type 'TArg'. """

    def create(self, Argument):
        """ Creates a new instance from the provided argument. """
        # Post:
        # Creates a new instance of type 'T', and returns it.
        raise NotImplementedError("Method 'IFactory.create' was not implemented.")
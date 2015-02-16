from IMap import *

class UserNameMap(IMap):
    """ A mapping function that maps users to their full names. """

    def __init__(self):
        """ Creates a new instance of a 'UserNameMap' mapping function. """
        pass

    def map(self, Customer):
        """ Maps the item to its target representation. """
        # Post:
        # This function must produce a constant return value, irrespective of external changes.
        return Customer.name
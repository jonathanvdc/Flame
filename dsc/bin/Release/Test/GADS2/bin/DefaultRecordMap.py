from IMap import *

class DefaultRecordMap(IMap):
    """ A mapping function that simply passes along the search key provided by the record. """

    def __init__(self):
        pass

    def map(self, Record):
        """ Maps the item to its target representation. """
        # Post:
        # This function must produce a constant return value, irrespective of external changes.
        return Record.key
class OpenHashtableItem:
    """ Describes an item in an open hash table. """

    def __init__(self, Value, IsEmpty):
        """ Creates a new item for use in an open hash table. """
        self.value = Value
        self.is_empty = IsEmpty
from IReadOnlyCollection import *

class IReadOnlyList(IReadOnlyCollection):
    """ Describes a generic read-only, zero-based list. """
    # Remarks:
    # The read-only list is useful for scenarios where a read-only copy of the data contained in another ADT is to be provided.
    # For example, a sorted list implementation that uses the list ADT internally could return an alias of said list, rather than a copy, when a position-oriented list version of its data is requested.
    # If the return type of the 'ToList()' method of the sorted list would have been a list, the client could reliably change the internal representation of the sorted list's data, destroying the integrity of the carefully constructed state of the sorted list.
    # Because the return type is a read-only list, however, clients know they should not try to (and indeed do not have a reliable way to) change the list's state.
    # This increases performance by removing unnecessary list copying without sacrificing encapsulation.
    # An extra requirement is that a read-only list implements the inherited base type 'iterable<T>' (from 'IReadOnlyCollection<T>') in a way that the item at the first index are iterated over in the order that they would be accessed by their indices: the item at index 0 should be indexed first, and the item at the end of the list should be indexed last.

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the item at said index is returned.
        # Otherwise, an exception is thrown.
        raise NotImplementedError("Method 'IReadOnlyList.__getitem__' was not implemented.")
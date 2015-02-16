from ICollection import *

class ISortedList(ICollection):
    """ Describes a modified version of the Sorted List ADT. """
    # Remarks:
    # Design changes from the original ADT: Rather than allowing the client of sorted lists to access its items through an index, this sorted list uses the ToList() method.
    # This change was introduced to improve efficiency for the common case, which is enumerating over the sorted list's items.
    # Implementing 'void sortedRetrieve(int index, out T dataItem, out bool success)' would require an increasingly large number of nodes to be counted through inorder traversal before arriving at the desired element when enumerating over a sorted list with a naive binary tree implementation.
    # Also, the 'int locatePosition(T anItem, out bool isPresent)' method has been removed, as this is essentially table functionality, and the position of an item cannot be retrieved unambiguously, since sorted lists may contain more than one item with the same search key. 'void sortedInsert(T anItem)' is handled by the inherited 'void Add(T Item)' method in ICollection<T>.

    def remove(self, Item):
        """ Removes an item from the list. """
        # Pre:
        # For Item to be successfully removed from the sorted list, it must have been in the list before removal.
        # Post:
        # Returns true if the sorted list contained the given item, false if not.
        # If this returns true, the item has been removed from the sorted list.
        raise NotImplementedError("Method 'ISortedList.remove' was not implemented.")

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        raise NotImplementedError("Method 'ISortedList.contains' was not implemented.")

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        raise NotImplementedError("Method 'ISortedList.to_list' was not implemented.")

    @property
    def key_map(self):
        """ Gets a record-to-key map that maps records to sortable keys. """
        raise NotImplementedError("Getter of property 'ISortedList.key_map' was not implemented.")

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the sorted list is empty. """
        # Post:
        # Return true if empty, false if not.
        raise NotImplementedError("Getter of property 'ISortedList.is_empty' was not implemented.")
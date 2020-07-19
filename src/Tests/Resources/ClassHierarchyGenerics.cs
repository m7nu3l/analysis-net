public interface InterfaceA<T> { }
public interface InterfaceB<T> { }
public interface InterfaceC<T> { }
public interface InterfaceD<T> : InterfaceC<T> { }

public class ClassA<T> { }
public class ClassB<T> : ClassA<T> { }
public class ClassC<T> : ClassB<T> { }
public class Example<T> : ClassC<T>, InterfaceA<T>, InterfaceB<int>, InterfaceD<T> { }


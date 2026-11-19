
USE biblioteca_municipal;
CREATE TABLE Autor (
    ID_Autor INT PRIMARY KEY,
    Nombre_Autor VARCHAR(100) NOT NULL,
    Nacionalidad VARCHAR(50)
);

CREATE TABLE Categoria (
    ID_Categoria INT PRIMARY KEY,
    Nombre_Categoria VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255)
);
CREATE TABLE Libro (
    ID_Libro INT PRIMARY KEY,
    Titulo VARCHAR(150) NOT NULL,
    Editorial VARCHAR(100),
    Anio_Publicacion INT,
    Genero VARCHAR(50),
    ID_Categoria INT,
    CONSTRAINT fk_libro_categoria FOREIGN KEY (ID_Categoria) REFERENCES Categoria(ID_Categoria)
);
CREATE TABLE Libro_Autor (
    ID_Libro INT,
    ID_Autor INT,
    PRIMARY KEY (ID_Libro, ID_Autor),
    CONSTRAINT fk_libro_autor_libro FOREIGN KEY (ID_Libro) REFERENCES Libro(ID_Libro),
    CONSTRAINT fk_libro_autor_autor FOREIGN KEY (ID_Autor) REFERENCES Autor(ID_Autor)
);

CREATE TABLE Lector (
    ID_Lector INT PRIMARY KEY,
    DNI_CI VARCHAR(20) NOT NULL UNIQUE,
    Nombre VARCHAR(50) NOT NULL,
    Apellido VARCHAR(50) NOT NULL,
    Telefono VARCHAR(20),
    Fecha_Inicio DATE
);
CREATE TABLE Prestamo (
    ID_Prestamo INT PRIMARY KEY,
    ID_Lector INT,
    ID_Libro INT,
    Fecha_Devolucion DATE,
    CONSTRAINT fk_prestamo_lector FOREIGN KEY (ID_Lector) REFERENCES Lector(ID_Lector),
    CONSTRAINT fk_prestamo_libro FOREIGN KEY (ID_Libro) REFERENCES Libro(ID_Libro)
);
CREATE TABLE Multa (
    ID_Multa INT PRIMARY KEY,
    ID_Prestamo INT,
    Fecha_Emision DATE,
    Monto DECIMAL(10, 2),
    Pagado DECIMAL(10,2),
    CONSTRAINT fk_multa_prestamo FOREIGN KEY (ID_Prestamo) REFERENCES Prestamo(ID_Prestamo)
);